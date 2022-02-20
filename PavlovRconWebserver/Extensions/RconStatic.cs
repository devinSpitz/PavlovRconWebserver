using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire.Annotations;
using Microsoft.VisualBasic;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PrimS.Telnet;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using Serilog.Events;
using TcpClient = System.Net.Sockets.TcpClient;

namespace PavlovRconWebserver.Extensions
{
    public static class RconStatic
    {
        private static async Task<string> SendCommandForShell(string customCmd, ShellStream stream,
            [CanBeNull] string expect, int ms = 2000)
        {
            var writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            var result = "";
            stream.Flush();
            await writer.FlushAsync();


            await writer.WriteLineAsync(customCmd);
            if (expect == null)
            {
                var reader = new StreamReader(stream);
                Task.Delay(ms).Wait();
                result = (await ReadStream(reader)).ToString();
            }
            else
            {
                result = stream.Expect(new Regex(expect, RegexOptions.Multiline), TimeSpan.FromMilliseconds(ms));
            }

            return result;
        }

        private static async Task<StringBuilder> ReadStream(StreamReader reader)
        {
            var result = new StringBuilder();

            string line;
            while ((line = await reader.ReadLineAsync()) != null) result.AppendLine(line);

            return result;
        }


        public static RconService.AuthType GetAuthType(SshServer server)
        {
            var auths = new List<RconService.AuthType>
            {
                RconService.AuthType.PrivateKeyPassphrase,
                RconService.AuthType.PrivateKey,
                RconService.AuthType.UserPass
            };
            foreach (var type in auths)
                try
                {
                    var connectionInfo = ConnectionInfoInternal(server, type, out var result);
                    using var client = new SshClient(connectionInfo);
                    client.Connect();
                    if (client.IsConnected)
                        return type;
                }
                catch (Exception)
                {
                    //ignore and find the right one to use
                }

            throw new CommandException("No ssh authentication method worked!");
        }

        public static void AuthenticateOnTheSshServer(SshServer server)
        {
            GetAuthType(server);
        }
        
        public static async Task<string> SystemDCheckState(PavlovServer server, IToastifyService notyfService)
        {
            var answer = "";
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);


                var state = await SendCommandForShell(
                    "systemctl  list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                    stream, @".*(enabled|disabled).*");
                if (state == null)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Service does not exist!" + server.Name,
                        LogEventLevel.Fatal, notyfService, result);
                }
                else
                {
                    if (state.Contains("disabled"))
                    {
                        answer = "disabled";
                    }
                    else if (state.Contains("enabled"))
                    {
                        var active = await SendCommandForShell(
                            "systemctl   is-active " + server.ServerSystemdServiceName + ".service", stream,
                            @"^(?!.*is-active).*active.*$");
                        if (active == null || active.Contains("inactive"))
                            answer = "inactive";
                        else
                            answer = "active";
                    }
                    else
                    {
                        var error =
                            "Service does not exist cause he is not enabled and not disabled!" +
                            server.Name;
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal, notyfService, result);
                        throw new CommandException(error);
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result, "SystemDCheckState",client);
            }
            finally
            {
                client.Disconnect();
            }

            return answer;
        }


        public static async Task<string> UpdateInstallPavlovServer(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                var error = " Steam is now enabled on this server!";
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                    LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                throw new CommandException(error);
            }

            var restart = false;
            if (server.ServerServiceState == ServerServiceState.active)
            {
                restart = true;
                await SystemDStop(server, pavlovServerService);
            }

            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);

                var version = "622970";
                if (server.Shack)
                    version += " -beta shack";
                var update = await SendCommandForShell(
                    "cd " + server.SshServer.SteamPath + " && ./steamcmd.sh +login anonymous +force_install_dir " +
                    server.ServerFolderPath + " +app_update "+version+" +exit", stream,
                    @".*(Success! App '622970' already up to date|Success! App '622970' fully installed).*", 120000);
                if (update == null)
                {
                    var error = "Could not update or install the pavlovserver " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }
                result.answer = update;
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result, "UpdateInstallPavlovServer -> ",client);
            }
            finally
            {
                client.Disconnect();
            }

            if (restart) await SystemDStart(server, pavlovServerService);

            return result.answer;
        }

        public static async Task<ConnectionResult> GetServerLog(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                //Todo make verbose logs
                client.Connect();
                var command = "journalctl -eu " + server.ServerSystemdServiceName + " -n 4000 -o cat --no-hostname --no-full -q";
                var content2 = client.RunCommand(command);
                if (content2 != null)
                {

                    result.answer = content2.Result;
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result, "GetServerLog -> ",client);
            }
            finally
            {
                client.Disconnect();
            }

            return result;
        }

        

        public static async Task SystemDStart(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var errorPrefix = "SystemDStart service -> ";
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                //Todo make verbose logs
                client.Connect();
                var stream = client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                
                //Check if the service does exist
                await EnableServiceWhenDisabled(server, pavlovServerService, stream, result);

                //start service
                var start = await SendCommandForShell(
                    "sudo /bin/systemctl restart " + server.ServerSystemdServiceName + ".service", stream, null);

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("started service result = " + start + " " + server.Name,
                    LogEventLevel.Verbose, pavlovServerService._notifyService);
                if (start == null)
                {
                    //so absolutly no answer even no added line
                    var error = errorPrefix+"Could not start service " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }
                if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                    {
                        var error = errorPrefix+"Could not start service after entering the password again!";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error
                            , LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);
                        throw new CommandException(error);
                    }

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "answer from entered password when trying to start: " + enteredPassword, LogEventLevel.Verbose,
                        pavlovServerService._notifyService);
                }
                
                //Should be fine cause we got a answer and it does not contain a password request
                //could get checked for any possible error that can happen
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(start, LogEventLevel.Verbose,
                    pavlovServerService._notifyService);

                if (start.ToLower().Contains("not found"))
                {
                    var error = errorPrefix+"Service was not found!¨"+server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error
                        , LogEventLevel.Fatal,
                        pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }
                    
            }
            catch (CommandException)
            {
                throw;
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result,errorPrefix, client);
            }
            finally
            {
                client.Disconnect();
            }

            //Check if started:
            var serverWithState = await pavlovServerService.GetServerServiceState(server);

            if (serverWithState.ServerServiceState != ServerServiceState.active)
            {
                var error = errorPrefix+" Server could not start! ";
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal,
                    pavlovServerService._notifyService, result);
                throw new CommandException(error);
            }

            await pavlovServerService.CheckStateForAllServers();
        }

        private static async Task EnableServiceWhenDisabled(PavlovServer server, PavlovServerService pavlovServerService,
            ShellStream stream, ConnectionResult result)
        {
            var disabled = await SendCommandForShell(
                "systemctl  list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                stream, @".*disabled.*");
            if (disabled != null)
            {
                //its disabled

                //try to enable
                var enable = await SendCommandForShell(
                    "sudo /bin/systemctl  enable " + server.ServerSystemdServiceName + ".service", stream, "Created symlink");
                if (enable == null)
                {
                    var error = "Could not enable service " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }

                if (enable.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, @".*[pP]assword.*");
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not enable service after entering the password again!", LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);

                    if (enteredPassword != null && enteredPassword.ToLower().Contains("password"))
                    {
                        var enteredPasswordReload = await SendCommandForShell(
                            server.SshServer.SshPassword, stream, null);
                        if (enteredPasswordReload == "\r\n" || enteredPasswordReload == null)
                        {
                            var error = "Could not enable service after entering the password again second try!";
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                error,
                                LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                            throw new CommandException(error);
                        }
                    }
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Didn't had the enter the password again the enable the server. Good info",
                        LogEventLevel.Verbose, pavlovServerService._notifyService);
                }
            }
        }

        public static async Task SystemDStop(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var errorPrefix = "SystemDStop server -> ";
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                
                await DisableServerIfEnabled(server, pavlovServerService, stream, result);

                //var start = await SendCommandForShell("systemctl stop " + server.ServerSystemdServiceName + ".service",
                var start = await SendCommandForShell(
                    "sudo /bin/systemctl  stop " + server.ServerSystemdServiceName + ".service",
                    stream, null);
                if (start == null)
                {
                    var error = errorPrefix+"Could not stop service " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                } 
                if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                    {
                        var error =  errorPrefix+"Could not stop service after entering the password !";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);
                        throw new CommandException(error);
                    }
                }
                //Should be fine cause we got a answer and it does not contain a password request
                //could get checked for any possible error that can happen
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(start, LogEventLevel.Verbose,
                    pavlovServerService._notifyService);

                if (start.ToLower().Contains("not found"))
                {
                    var error = errorPrefix+" Service was not found!¨"+server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error
                        , LogEventLevel.Fatal,
                        pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }
            }
            catch (CommandException)
            {
                throw;
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result,errorPrefix, client);
            }
            finally
            {
                client.Disconnect();
            }

            var serverWithState = await pavlovServerService.GetServerServiceState(server);

            if (serverWithState.ServerServiceState == ServerServiceState.active)
            {
                var error = errorPrefix +"Server could not stop! ";
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal,
                    pavlovServerService._notifyService, result);
                throw new CommandException(error);

            }

            await pavlovServerService.CheckStateForAllServers();
        }

        private static async Task DisableServerIfEnabled(PavlovServer server, PavlovServerService pavlovServerService,
            ShellStream stream, ConnectionResult result)
        {
            var disabled = await SendCommandForShell(
                "systemctl  list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                stream, @".*enabled.*");
            if (disabled != null)
            {
                var enable = await SendCommandForShell(
                    "sudo /bin/systemctl disable " + server.ServerSystemdServiceName + ".service", stream,
                    null);
                if (enable == null)
                {
                    var error = "Could not disable service " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    throw new CommandException(error);
                }
                
                
                if (enable.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, @".*[pP]assword.*");
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                    {
                        var error = 
                            "Could not disable service after entering the password!";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                            LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                        throw new CommandException(error);
                    }

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "answer from entered password when trying to start: " + enteredPassword,
                        LogEventLevel.Verbose, pavlovServerService._notifyService);
                }
            }
        }


        public static ConnectionResult DeleteUnusedMaps(PavlovServer server,
            List<ServerSelectedMap> serverSelectedMaps)
        {
            var connectionResult = new ConnectionResult();
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            client.Connect();
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            var toDeleteMaps = new List<string>();
            try
            {
                sftp.Connect();
                //Delete old maps in tmp folder
                //
                var folderPatch = "/tmp/workshop/" + server.ServerPort + "/content/555160";
                if (server.Shack)
                {
                    folderPatch = server.ServerFolderPath + "Pavlov/Saved/maps";
                }
                if (sftp.Exists(folderPatch))
                {
                    var maps = sftp.ListDirectory(folderPatch);
                    foreach (var map in maps)
                    {
                        if (!map.IsDirectory) continue;
                        if (map.Name == ".") continue;
                        if (map.Name == "..") continue;
                        if (serverSelectedMaps.FirstOrDefault(x => x.Id == server.Id && x.Map.Name == map.Name) != null
                        ) // map is on the selectet list
                            continue; // map is selected

                        // Check if map is running
                        var isRunningAnswerCommand = client.CreateCommand("lsof +D " + map.FullName);
                        isRunningAnswerCommand.CommandTimeout = TimeSpan.FromMilliseconds(2000);
                        var isRunningAnswer = isRunningAnswerCommand.Execute();
                        if (isRunningAnswer.Contains("COMMAND") && isRunningAnswer.Contains("USER")
                        ) // map is running on the server
                            continue; // map is in use

                        //Check usage
                        SftpFileAttributes attributes = null;
                        try
                        {
                            attributes = sftp.GetAttributes(map.FullName + "/LinuxServer.pak");
                        }
                        catch (SftpPathNotFoundException)
                        {
                            continue;
                        }

                        var lastAccessTime = attributes.LastAccessTime;
                        if (lastAccessTime < DateTime.Now.Subtract(new TimeSpan(server.DeletAfter, 0, 0, 0)))
                        {
                            try
                            {
                                client.RunCommand("rm -rf " + map.FullName);
                            }
                            catch (SftpPermissionDeniedException)
                            {
                                sftp.Disconnect();
                                throw new CommandException("Permission denied to delet map" + server.Name);
                            }

                            if (sftp.Exists(map.FullName))
                            {
                                sftp.Disconnect();
                                throw new CommandException("Could not delete map!" + server.Name);
                            }
                        }
                    }
                }
            }
            finally
            {
                sftp.Disconnect();
            }

            client.Disconnect();
            connectionResult.Success = true;
            return connectionResult;
        }
        
        public static ConnectionResult CopyNeededMapsToShackServer(PavlovServer pavlovServer,ServerSelectedMap[] serverSelectedMaps)
        {
            var connectionResult = new ConnectionResult();
            var type = GetAuthType(pavlovServer.SshServer);
            var connectionInfo = ConnectionInfoInternal(pavlovServer.SshServer, type, out var result);
            using var client = new SshClient(connectionInfo);
            client.Connect();
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();
                foreach (var serverSelectedMap in serverSelectedMaps)
                {
                    var mapsPath = pavlovServer.ServerFolderPath + "Pavlov/Saved/maps/";
                    if(!sftp.Exists(mapsPath))
                        sftp.CreateDirectory(mapsPath.Substring(0,mapsPath.Length-1));
                    
                    if (sftp.Exists(pavlovServer.SshServer.ShackMapsPath+serverSelectedMap.Map.Name))
                    {
                        var shackFolderPath =  mapsPath +
                                               serverSelectedMap.Map.Name;
                        if(sftp.Exists(shackFolderPath))
                            client.RunCommand("rm -rf "+shackFolderPath);
                        
                        sftp.CreateDirectory(shackFolderPath);
                        
                        client.RunCommand("cp -r "+pavlovServer.SshServer.ShackMapsPath+serverSelectedMap.Map.Name+"/* "+ shackFolderPath);
                    }
                }
            }
            finally
            {
                sftp.Disconnect();
            }

            client.Disconnect();
            connectionResult.Success = true;
            return connectionResult;
        }

        
        public static ConnectionResult GetAFolderList(SshServer server,string path)
        {
            var connectionResult = new ConnectionResult();
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            client.Connect();
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();
                //Delete old maps in tmp folder
                //
                if (sftp.Exists(path))
                {
                    connectionResult.answer=string.Join(";",sftp.ListDirectory(path).Where(x=>x.IsDirectory).Select(x=>x.Name));
                }
            }
            finally
            {
                sftp.Disconnect();
            }

            client.Disconnect();
            connectionResult.Success = true;
            return connectionResult;
        }

        public static async Task InstallPavlovServerService(PavlovServer server, IToastifyService notyfService)
        {
            var errorPrefix = "Could not install the pavlov server service -> ";
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                result.Success = false;
                var error = errorPrefix+"Steam is not enabled on this server!";
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                    LogEventLevel.Fatal, notyfService, result);
                throw new CommandException(error);
            }

            using var client = new SshClient(connectionInfo);
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                var serviceTempalte =
                    @"[Unit]
Description=Pavlov VR dedicated server

[Service]
Type=simple
WorkingDirectory=" + server.ServerFolderPath + @"
ExecStart=" + server.ServerFolderPath + @"PavlovServer.sh  -PORT='" + server.ServerPort + @"'

RestartSec=1
Restart=always
User=" + server.SshServer.NotRootSshUsername + @"
Group=" + server.SshServer.NotRootSshUsername + @"

[Install]
WantedBy = multi-user.target";

                sftp.BufferSize = 4 * 1024; // bypass Payload error large files
                sftp.Connect();
                //var path = "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service";

                //these folders are not always presend.

                var path = "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service";
                //check if file exist
                if (sftp.Exists(path)) sftp.DeleteFile(path);

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("try to upload service!", LogEventLevel.Verbose,
                    notyfService);
                await using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(serviceTempalte)))
                {
                    sftp.UploadFile(fileStream, path);
                }

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Uploaded service!", LogEventLevel.Verbose,
                    notyfService);
                //Download file again to valid result
                var outPutStream = new MemoryStream();
                await using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
                }


                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Downloaded service again!",
                    LogEventLevel.Verbose, notyfService);
                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace("\n", "") != serviceTempalte.Replace("\n", ""))
                {
                    
                    var error = errorPrefix+" Check showed that the file that should get uploaded got corrupted or could not get uploaded!";
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Verbose,
                        notyfService);
                    result.Success = false;
                    
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, notyfService, result);
                    
                    throw new CommandException(error);
                }

                //
                // daemon reload
                //Do not handle errors right now cause that could be different on every os and possibly does not return anything like its normal when it worked
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                var state = await SendCommandForShell("systemctl  daemon-reload", stream, null);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Daemon reload result: " + state, LogEventLevel.Verbose,
                    notyfService);
                
                

                

                //add own sudoers file if needed:
                var sudoersPathParent = "/etc/sudoers.d";

                var sudoersPath = sudoersPathParent + "/pavlovRconWebserverManagement";

                if (!sftp.Exists(sudoersPathParent))
                {
                    var error = errorPrefix+" Could not add own sudoers file! Check if the " + sudoersPathParent +
                                " exists and get loaded in the sudoers file!";
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal, notyfService, result);
                    throw new CommandException(error);
                }

                if (!sftp.Exists(sudoersPath))
                {
                    try
                    {
                        sftp.Create(sudoersPath);
                    }
                    catch (Exception e)
                    {
                        throw new CommandException(errorPrefix+e.Message);
                    }
                    if (!sftp.Exists(sudoersPath))
                    {
                        var error = errorPrefix+
                                    "Could not create the sudoers file on the path -> " + sudoersPath;
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error
                            , LogEventLevel.Fatal, notyfService,
                            result);
                        throw new CommandException(error);
                    }
                }


                // add line
                try
                {
                    AddServerLineToSudoersFile(server, notyfService, sudoersPath, result);
                }
                catch (CommandException e)
                {
                    throw new CommandException(errorPrefix+e.Message);
                }

                
                var justToMakeSureSudoKnowsTheChanges = await SendCommandForShell("sudo su", stream, null);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                    "sudo su answer after changes from the sudoers file: " + justToMakeSureSudoKnowsTheChanges,
                    LogEventLevel.Verbose, notyfService);
                
                //handle presets for new systemd in like arch
                AddLineToNewerSystemDsIfNeeded(server,notyfService);
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result,errorPrefix, client, sftp);
            }
            finally
            {
                client.Disconnect();
                sftp.Disconnect();
            }
            
        }

        public static bool RemoveServerLineToSudoersFile(PavlovServer server, IToastifyService notyfService,
            string sudoersPath, PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);

            using var clientSftp = new SftpClient(connectionInfo);


            var success = false;
            try
            {
                clientSftp.Connect();


                var sudoersLine = SudoersLine(server);
                var sudoers = clientSftp.ReadAllText(sudoersPath);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("sudoers content: " + sudoers, LogEventLevel.Verbose,
                    notyfService);
                if (!sudoers.Contains(sudoersLine))
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("the server is removed from sudoers file.",
                        LogEventLevel.Verbose, notyfService);
                    success = true;
                }
                else
                {
                    var sudoersFileContent = clientSftp.ReadAllLines(sudoersPath);
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "sudoers content: " + string.Join("\n", sudoersFileContent),
                        LogEventLevel.Verbose, notyfService);

                    for (var i = 0; i < sudoersFileContent.Length; i++)
                        if (sudoersFileContent[i].Trim().Contains(sudoersLine.Trim()))
                        {
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Found line and replace it with emptyness",
                                LogEventLevel.Verbose, notyfService);
                            sudoersFileContent[i] = "";
                        }
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("recreate the sudoers file", LogEventLevel.Verbose,
                        notyfService);
                    clientSftp.Create(sudoersPath);

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("refill the sudoers file", LogEventLevel.Verbose,
                        notyfService);
                    clientSftp.WriteAllLines(sudoersPath, sudoersFileContent.Where(x => x != ""));
                    var sudoersFileContentAfterRemove = clientSftp.ReadAllLines(sudoersPath);
                    if (sudoersFileContentAfterRemove.Contains(sudoersLine))
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not remove server line from the sudoers file!", LogEventLevel.Fatal,
                            notyfService);
                    else
                        success = true;
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result, "RemoveServerLineToSudoersFile -> ", null, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
            }

            return success;
        }

        /// <summary>
        /// In Arch linux the Preset state is shown cause it has a newer release of systemd.
        /// The workaround is to add the pavlovServer to the preset file so the state should get dedected right again.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="notyfService"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static bool AddLineToNewerSystemDsIfNeeded(PavlovServer server, IToastifyService notyfService, bool remove = false)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);

            using var clientSftp = new SftpClient(connectionInfo);
            using var clientSsh = new SshClient(connectionInfo);
            string[] presetsPaths = {
                "/usr/lib/systemd/system-preset/90-systemd.preset",
                "/usr/lib/systemd/system-preset/99-default.preset",
            };

            var success = false;
            try
            {
                clientSftp.Connect();
                clientSsh.Connect();
                
                var presetString = systemPresetForNeweSystemds(server);
                
                
                foreach (var presetPath in presetsPaths)
                {
                    
                    if(!clientSftp.Exists(presetPath)) continue;
                    
                    
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Check system service presets that exist otherwise the system will detect the server state wrong.",
                        LogEventLevel.Verbose, notyfService);
                    if (remove)
                    {
                        var presetLines = clientSftp.ReadAllLines(presetPath);
                        if (presetLines.Contains(presetString))
                        {
                            clientSftp.WriteAllLines(presetPath,presetLines.Where(x=>x.Trim()!=presetString.Trim()));
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Did remove the system preset that for the pavlovServer.",
                                LogEventLevel.Verbose, notyfService);
                        }
                    }
                    else
                    {
                        var presetLines = clientSftp.ReadAllLines(presetPath);
                        if (!presetLines.Contains(presetString.Trim()))
                        {
                            clientSftp.WriteAllLines(presetPath,presetLines.Append(presetString));
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Did add the system preset that for the pavlovServer.",
                                LogEventLevel.Verbose, notyfService);
                        }
                    }

                    success = true;

                    
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Presets should be fine",
                        LogEventLevel.Verbose, notyfService);
                }


            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result, "AddLineToNewerSystemDsIfNeeded -> ",clientSsh, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
                clientSsh.Disconnect();
            }

            return success;
        }

        private static string systemPresetForNeweSystemds(PavlovServer server)
        {
            var systemPresetString = "enable "+ server.ServerSystemdServiceName;
            return systemPresetString;
        }

        
        public static void AddServerLineToSudoersFile(PavlovServer server, IToastifyService notyfService,
            string sudoersPath, ConnectionResult connectionResult)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);

            using var clientSftp = new SftpClient(connectionInfo);
            using var clientSsh = new SshClient(connectionInfo);


            var success = false;
            try
            {
                clientSftp.Connect();
                clientSsh.Connect();
                var sudoersLine = SudoersLine(server);
                var sudoers = clientSftp.ReadAllLines(sudoersPath);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("sudoers content: " + sudoers, LogEventLevel.Verbose,
                    notyfService);
                if (!sudoers.Contains(sudoersLine))
                {
                    var tmpList = sudoers.ToList();
                    tmpList.Add(sudoersLine);
                    clientSftp.WriteAllLines(sudoersPath, tmpList);
                    var afterAdding = clientSftp.ReadAllText(sudoersPath);
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("sudoers content after adding line: " + afterAdding,
                        LogEventLevel.Verbose, notyfService);

                    if (!afterAdding.Contains(sudoersLine))
                    {
                        var error = "Could not add line to the sudoers file to start and stop the server!";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal,
                            notyfService, connectionResult);
                        throw new CommandException(error);
                    }
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("the server is already in the sudoers file.",
                        LogEventLevel.Verbose, notyfService);
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result, "AddServerLineToSudoersFile -> ",clientSsh, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
                clientSsh.Disconnect();
            }

        }

        private static string SudoersLine(PavlovServer server)
        {
            var sudoersLine = server.SshServer.NotRootSshUsername + " ALL= (root) NOPASSWD: /bin/systemctl stop " +
                              server.ServerSystemdServiceName + ".service, /bin/systemctl restart " +
                              server.ServerSystemdServiceName + ".service, /bin/systemctl status " +
                              server.ServerSystemdServiceName + ".service, /bin/systemctl enable " +
                              server.ServerSystemdServiceName + ".service, /bin/systemctl disable " +
                              server.ServerSystemdServiceName + ".service";
            return sudoersLine;
        }

        public static async Task RemovePath(PavlovServer server, string path,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            var restart = false;
            if (server.ServerServiceState == ServerServiceState.active)
            {
                restart = true;
                await SystemDStop(server, pavlovServerService);
            }

            using var client = new SshClient(connectionInfo);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                client.Connect();
                clientSftp.Connect();
                if (clientSftp.Exists(path))
                {
                    var stream =
                        client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                    var update = await SendCommandForShell(
                        "rm -rf " + path, stream, null);
                    if (update == null)
                    {
                        var error =
                            "Could not remove the " + path + "! " + server.Name;
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);
                        throw new CommandException(error);
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, pavlovServerService._notifyService, e, result, "RemovePath -> ",client, clientSftp);
            }
            finally
            {
                client.Disconnect();
                clientSftp.Disconnect();
            }

            if (restart) await SystemDStart(server, pavlovServerService);

        }

        public static void ExceptionHandlingSshSftp(string serverName, IToastifyService notyfService, Exception e,
            ConnectionResult result,string prefixError, SshClient client = null, SftpClient clientSftp = null)
        {
            
            client?.Disconnect();
            clientSftp?.Disconnect();
            switch (e)
            {
                case SshAuthenticationException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+"Could not Login over ssh!" + serverName,
                        LogEventLevel.Fatal, notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
                case SshConnectionException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+"Could not connect to host over ssh!" + serverName,
                        LogEventLevel.Fatal, notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
                case SshOperationTimeoutException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+
                                                                    "Could not connect to host cause of timeout over ssh!" + serverName, LogEventLevel.Fatal,
                        notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
                case SocketException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+"Could not connect to host!" + serverName,
                        LogEventLevel.Fatal, notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
                case InvalidOperationException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+
                                                                    e.Message + " <- most lily this error is from telnet" + serverName, LogEventLevel.Fatal,
                        notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
                //how to just rethrow here without loosing the stack strace?
                case not null:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(prefixError+
                                                                    e.Message + " <- most lily this error is from telnet" + serverName, LogEventLevel.Fatal,
                        notyfService, result);
                    throw new CommandException(prefixError+" "+e.Message);
            }
        }

        public static bool DoesPathExist(PavlovServer server, string path, IToastifyService notyfService)
        {
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                clientSftp.Connect();
                if (clientSftp.Exists(path))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result, "DoesPathExist -> ",null, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
            }

            var error = "Could not check if the server does exist!";
            DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                LogEventLevel.Fatal, notyfService, result);
            throw new CommandException(error);
        }
        public static int GetAvailablePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static async Task<string[]> SShTunnelMultipleCommands(PavlovServer server,
            string[] commands, IToastifyService notyfService)
        {
            var answerList = new List<string>();
            var result = StartClient(server, out var client);
            try
            {
                client.Connect();

                if (client.IsConnected)
                {
                    var nextFreePort = GetAvailablePort();
                    var portToForward = nextFreePort;
                    var portForwarded = new ForwardedPortLocal("127.0.0.1", (uint) portToForward, "127.0.0.1",
                        (uint) server.TelnetPort);
                    client.AddForwardedPort(portForwarded);
                    portForwarded.Start();
                    using (var client2 = new Client("127.0.0.1", portToForward, new CancellationToken()))
                    {
                        if (client2.IsConnected)
                        {
                            var password = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Answer: " + password,
                                LogEventLevel.Verbose, notyfService);
                            if (password.ToLower().Contains("password"))
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("start sending password!",
                                    LogEventLevel.Verbose, notyfService);
                                await client2.WriteLine(server.TelnetPassword);
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("did send password and wait for auth",
                                    LogEventLevel.Verbose, notyfService);
                                var auth = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify("waited for auth got : " + auth,
                                    LogEventLevel.Verbose, notyfService);
                                if (auth.ToLower().Contains("authenticated=1"))
                                {
                                    foreach (var command in commands)
                                    {
                                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("send command: " + command,
                                            LogEventLevel.Verbose, notyfService);
                                        var singleCommandResult = await SingleCommandResult(client2, command);
                                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                            "Got answer: " + singleCommandResult, LogEventLevel.Verbose, notyfService);
                                        answerList.Add(singleCommandResult);
                                    }
                                }
                                else
                                {
                                    var error = "Telnet Client could not authenticate ..." + server.Name;
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                                        LogEventLevel.Fatal, notyfService, result);
                                    throw new CommandException(error);
                                }
                            }
                            else
                            {
                                var error ="Telnet Client did not ask for Password ..." + server.Name;
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                                    LogEventLevel.Fatal, notyfService, result);
                                throw new CommandException(error);
                            }
                        }
                        else
                        {
                            var error ="Telnet Client could not connect ..." + server.Name;
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                                LogEventLevel.Fatal, notyfService, result);
                            throw new CommandException(error);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    var error ="Telnet Client cannot be reached...";
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                        LogEventLevel.Fatal, notyfService, result);
                    throw new CommandException(error);
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingSshSftp(server.Name, notyfService, e, result, "MultiSShCommands -> ",client);
            }
            finally
            {
                client.Disconnect();
            }

            if (answerList.Count <= 0)
            {
                var error = "there was no answer" + server.Name;
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(error,
                    LogEventLevel.Fatal, notyfService, result);
                throw new CommandException(error);
            }


            return answerList.ToArray();
        }

        public static ConnectionResult StartClient(PavlovServer server, out SshClient client)
        {
            if (server.ServerServiceState != ServerServiceState.active)
                throw new CommandException("will not do command while server service is inactive!");
            var type = GetAuthType(server.SshServer);
            var connectionInfo = ConnectionInfoInternal(server.SshServer, type, out var result);
            client = new SshClient(connectionInfo);
            return result;
        }

        public static async Task<string> SingleCommandResult(Client client2, string command)
        {
            await client2.WriteLine(command);
            var commandResult = await client2.ReadAsync(TimeSpan.FromMilliseconds(2000));


            var singleCommandResult = "";
            if (commandResult.Contains("{"))
                singleCommandResult = commandResult
                    .Substring(commandResult.IndexOf("{", StringComparison.Ordinal));

            if (singleCommandResult.StartsWith("Password: Authenticated=1"))
                singleCommandResult = singleCommandResult.Replace("Password: Authenticated=1", "");


            if (singleCommandResult.Contains(command))
                singleCommandResult = singleCommandResult.Replace(command, "");
            return singleCommandResult;
        }

        public static async Task<string> SendCommandSShTunnel(PavlovServer server, string command,
            IToastifyService notyfService)
        {
            var result = await SShTunnelMultipleCommands(server, new[] {command}, notyfService);
            return Strings.Join(result.ToArray(), "\n");
        }

        public static ConnectionInfo ConnectionInfoInternal(SshServer server, RconService.AuthType type,
            out ConnectionResult result)
        {
            ConnectionInfo connectionInfo = null;

            result = new ConnectionResult();
            //auth
            if (type == RconService.AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile(new MemoryStream(server.SshKeyFileName))};
                connectionInfo = new ConnectionInfo(server.Adress, server.SshPort,
                    server.SshUsername,
                    new PrivateKeyAuthenticationMethod(server.SshUsername, keyFiles));
            }
            else if (type == RconService.AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(server.Adress, server.SshPort,
                    server.SshUsername,
                    new PasswordAuthenticationMethod(server.SshUsername, server.SshPassword));
            }
            else if (type == RconService.AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[]
                    {new PrivateKeyFile(new MemoryStream(server.SshKeyFileName), server.SshPassphrase)};
                connectionInfo = new ConnectionInfo(server.Adress, server.SshPort,
                    server.SshUsername,
                    new PasswordAuthenticationMethod(server.SshUsername, server.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(server.SshUsername, keyFiles));
            }

            return connectionInfo;
        }

        public static string GetFile(SshServer server, string path, IToastifyService notyfService)
        {
            var fileContent = "";
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();

                var outPutStream = new MemoryStream();
                try
                {
                    //check if file exist
                    if (!sftp.Exists(path))
                    {
                        sftp.Disconnect();
                        return fileContent;
                    }

                    //Download file
                    using Stream fileStream = outPutStream;
                    sftp.DownloadFile(path, fileStream);
                }
                catch (Exception e)
                {
                    ExceptionHandlingSshSftp(server.Name, notyfService, e, result,"GetFile -> ",null,sftp);
                }

                var fileContentArray = outPutStream.ToArray();
                fileContent = Encoding.Default.GetString(fileContentArray);
            }
            finally
            {
                sftp.Disconnect();
            }


            return fileContent;
        }

        public static void WriteFile(SshServer server, string path, string[] content, IToastifyService notyfService)
        {
            var connectionResult = new ConnectionResult();
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            sftp.BufferSize = 4 * 1024; // bypass Payload error large files
            var outPutStream = new MemoryStream();
            try
            {
                sftp.Connect();
                //check if file exist
                try
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("remove if file already exist",
                        LogEventLevel.Verbose, notyfService);
                    if (sftp.Exists(path)) sftp.DeleteFile(path);


                    //check if parent folder exist
                    var parentDirString = "";

                    if (path.EndsWith("/"))
                        path = path.TrimEnd('/');

                    var idx = path.LastIndexOf('/');

                    if (idx != -1)
                        parentDirString = path.Substring(0, idx);
                    else
                        parentDirString = path;

                    if (!sftp.Exists(parentDirString))
                    {
                        var error = "Can not write file when the parent folder does not exist!";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(error, LogEventLevel.Fatal, notyfService,connectionResult);
                        throw new CommandException(error);
                    }

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("create file", LogEventLevel.Verbose, notyfService);
                    if (!sftp.Exists(path)) sftp.Create(path);

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("fill the file", LogEventLevel.Verbose,
                        notyfService);
                    
                    sftp.WriteAllLines(path, content);


                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Uploaded finish now download file",
                        LogEventLevel.Verbose, notyfService);
                    //Download file again to valid result
                    using (Stream fileStream = outPutStream)
                    {
                        sftp.DownloadFile(path, fileStream);
                    }

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Download finish now compare",
                        LogEventLevel.Verbose, notyfService);
                }
                catch (CommandException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ExceptionHandlingSshSftp(server.Name, notyfService, e, result,"WriteFile -> ",null,sftp);
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace("\n", "").Replace("\r", "").Trim() == string.Join("",content).Replace("\n", "").Replace("\r", "").Trim())
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Upload complet finished. also checked and its the same", LogEventLevel.Verbose, notyfService);
                }
                else
                {
                    var error = "File in not the same as uploaded. So upload failed! " + server.Name;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(error
                        , LogEventLevel.Fatal,
                        notyfService, connectionResult);
                    throw new CommandException(error);
                }
            }
            finally
            {
                sftp.Disconnect();
            }

        }
    }
}