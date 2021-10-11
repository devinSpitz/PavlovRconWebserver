using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        public static RconService.AuthType GetAuthType(PavlovServer server)
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

            throw new CommandException("No auth method worked!");
        }

        public static async Task<string> SystemDCheckState(PavlovServer server, IToastifyService notyfService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
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
                        result.answer = "disabled";
                    }
                    else if (state.Contains("enabled"))
                    {
                        var active = await SendCommandForShell(
                            "systemctl   is-active " + server.ServerSystemdServiceName + ".service", stream,
                            @"^(?!.*is-active).*active.*$");
                        if (active == null || active.Contains("inactive"))
                            result.answer = "inactive";
                        else
                            result.answer = "active";
                    }
                    else
                    {
                        result.answer = "notAvailable";
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Service does not exist cause he is not enabled and not disabled!" +
                            server.Name, LogEventLevel.Fatal, notyfService, result);
                    }
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, notyfService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;
            return EndConnection(result);
        }


        public static async Task<string> UpdateInstallPavlovServer(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                result.Success = false;
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(" Steam is now enabled on this server!",
                    LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                return EndConnection(result);
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
                var update = await SendCommandForShell(
                    "cd " + server.SshServer.SteamPath + " && ./steamcmd.sh +login anonymous +force_install_dir " +
                    server.ServerFolderPath + " +app_update 622970 +exit", stream,
                    @".*(Success! App '622970' already up to date|Success! App '622970' fully installed).*", 60000);
                if (update == null)
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not update the pavlovserver " + server.Name,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                result.answer = update;
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, pavlovServerService._notifyService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            if (restart) await SystemDStart(server, pavlovServerService);

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;


            return EndConnection(result);
        }


        public static async Task<ConnectionResult> SystemDStart(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                //Todo make verbose logs
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                var disabled = await SendCommandForShell(
                    "systemctl  list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                    stream, @".*disabled.*");
                if (disabled != null)
                {
                    var enable = await SendCommandForShell(
                        "sudo /bin/systemctl  enable " + server.ServerSystemdServiceName + ".service", stream, null);
                    if (enable == null)
                    {
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not enable service " + server.Name,
                            LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    }
                    else if (enable.ToLower().Contains("password"))
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
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                    "Could not enable service after entering the password again second try!",
                                    LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                        }
                    }
                    else
                    {
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Didn't had the enter the password again the enable the server. Good info",
                            LogEventLevel.Verbose, pavlovServerService._notifyService);
                    }

                    //enable for reload
                }

                var start = await SendCommandForShell(
                    "sudo /bin/systemctl restart " + server.ServerSystemdServiceName + ".service", stream, null);

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("started service result = " + start + " " + server.Name,
                    LogEventLevel.Verbose, pavlovServerService._notifyService);
                if (start == null)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not start service " + server.Name,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                }
                else if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not start service after entering the password again!", LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "answer from entered password when trying to start: " + enteredPassword, LogEventLevel.Verbose,
                        pavlovServerService._notifyService);
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(start, LogEventLevel.Verbose,
                        pavlovServerService._notifyService);
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, pavlovServerService._notifyService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            //Check if started:
            var serverWithState = await pavlovServerService.GetServerServiceState(server);

            if (serverWithState.ServerServiceState != ServerServiceState.active)
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Server could not start! ", LogEventLevel.Fatal,
                    pavlovServerService._notifyService, result);


            if (result.errors.Count <= 0) result.Success = true;

            await pavlovServerService.CheckStateForAllServers();
            return result;
        }

        public static async Task<ConnectionResult> SystemDStop(PavlovServer server,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
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
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not disable service " + server.Name,
                            LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                    }
                    else if (enable.ToLower().Contains("password"))
                    {
                        var enteredPassword = await SendCommandForShell(
                            server.SshServer.SshPassword, stream, @".*[pP]assword.*");
                        if (enteredPassword == "\r\n" || enteredPassword == null)
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Could not disable service after entering the password again!", LogEventLevel.Fatal,
                                pavlovServerService._notifyService, result);
                        if (enteredPassword != null && enteredPassword.ToLower().Contains("password"))
                        {
                            var enteredPasswordReload = await SendCommandForShell(
                                server.SshServer.SshPassword, stream, null);
                            if (enteredPasswordReload == "\r\n" || enteredPasswordReload == null)
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                    "Could not disable service after entering the password again second try!",
                                    LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                        }

                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "answer from entered password when trying to start: " + enteredPassword,
                            LogEventLevel.Verbose, pavlovServerService._notifyService);
                    }
                }

                //var start = await SendCommandForShell("systemctl stop " + server.ServerSystemdServiceName + ".service",
                var start = await SendCommandForShell(
                    "sudo /bin/systemctl  stop " + server.ServerSystemdServiceName + ".service",
                    stream, null);
                if (start == null)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not stop service " + server.Name,
                        LogEventLevel.Fatal, pavlovServerService._notifyService, result);
                }
                else if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not stop service after entering the password again!", LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(start, LogEventLevel.Verbose,
                        pavlovServerService._notifyService);
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, pavlovServerService._notifyService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            var serverWithState = await pavlovServerService.GetServerServiceState(server);

            if (serverWithState.ServerServiceState == ServerServiceState.active)
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Server could not stop! ", LogEventLevel.Fatal,
                    pavlovServerService._notifyService, result);

            if (result.errors.Count <= 0) result.Success = true;
            await pavlovServerService.CheckStateForAllServers();
            return result;
        }


        public static ConnectionResult DeleteUnusedMaps(PavlovServer server,
            List<ServerSelectedMap> serverSelectedMaps)
        {
            // Ned to check
            //1. Running Maps
            //2. May not used for 48h?
            // //Cause all server shares the same save space for maps.
            // return new ConnectionResult()
            // {
            //     Seccuess = true,
            //     answer = "Did nothing"
            // };
            var connectionResult = new ConnectionResult();
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
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
                if (sftp.Exists("/tmp/workshop/" + server.ServerPort + "/content/555160"))
                {
                    var maps = sftp.ListDirectory("/tmp/workshop/" + server.ServerPort + "/content/555160");
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
                                DeleteDirectory(sftp, map.FullName);
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

        public static async Task<string> InstallPavlovServerService(PavlovServer server, IToastifyService notyfService,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                result.Success = false;
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Steam is not enabled on this server!",
                    LogEventLevel.Fatal, notyfService, result);
                return EndConnection(result);
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


                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Downloaded service again service!",
                    LogEventLevel.Verbose, notyfService);
                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace(Environment.NewLine, "") != serviceTempalte.Replace(Environment.NewLine, ""))
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Files are not the same!", LogEventLevel.Verbose,
                        notyfService);
                    result.Success = false;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not upload service file!",
                        LogEventLevel.Fatal, notyfService, result);
                    return EndConnection(result);
                }

                //
                // //daemon reload
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
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Could not add own sudoers file! Check if the " + sudoersPathParent +
                        " exists and get loaded in the sudoers file!", LogEventLevel.Fatal, notyfService, result);
                    return EndConnection(result);
                }

                if (!sftp.Exists(sudoersPath))
                {
                    sftp.Create(sudoersPath);
                    if (!sftp.Exists(sudoersPath))
                    {
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not create the sudoers file: " + sudoersPath, LogEventLevel.Fatal, notyfService,
                            result);
                        return EndConnection(result);
                    }
                }


                // add line
                var success =
                    AddServerLineToSudoersFile(server, notyfService, pavlovServerService, sudoersPath, result);

                if (!success) return EndConnection(result);


                var justToMakeSureSudoKnowsTheChanges = await SendCommandForShell("sudo su", stream, null);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                    "sudo su answer after changes from the sudoersfile: " + justToMakeSureSudoKnowsTheChanges,
                    LogEventLevel.Verbose, notyfService);
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, notyfService, e, result, client, sftp);
            }
            finally
            {
                client.Disconnect();
                sftp.Disconnect();
            }


            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return EndConnection(result);
        }

        public static bool RemoveServerLineToSudoersFile(PavlovServer server, IToastifyService notyfService,
            string sudoersPath, PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);

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
                    clientSftp.AppendAllLines(sudoersPath, sudoersFileContent.Where(x => x != ""));


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
                ExcpetionHandlingSshSftp(server, pavlovServerService._notifyService, e, result, null, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
            }

            return success;
        }

        public static bool AddServerLineToSudoersFile(PavlovServer server, IToastifyService notyfService,
            PavlovServerService pavlovServerService,
            string sudoersPath, ConnectionResult connectionResult)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);

            using var clientSftp = new SftpClient(connectionInfo);
            using var clientSsh = new SshClient(connectionInfo);


            var success = false;
            try
            {
                clientSftp.Connect();
                clientSsh.Connect();

                var sudoersLine = SudoersLine(server);
                var sudoers = clientSftp.ReadAllText(sudoersPath);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("sudoers content: " + sudoers, LogEventLevel.Verbose,
                    notyfService);
                if (!sudoers.Contains(sudoersLine))
                {
                    clientSftp.AppendAllLines(sudoersPath, new[] {sudoersLine});

                    var afterAdding = clientSftp.ReadAllText(sudoersPath);
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("sudoers content after adding line: " + afterAdding,
                        LogEventLevel.Verbose, notyfService);

                    if (!afterAdding.Contains(sudoersLine))
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not add line to the sudoers file to start and stop the server!", LogEventLevel.Fatal,
                            notyfService, connectionResult);
                    else
                        success = true;
                }
                else
                {
                    success = true;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("the server is already in the sudoers file.",
                        LogEventLevel.Verbose, notyfService);
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, notyfService, e, result, clientSsh, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
                clientSsh.Disconnect();
            }

            return success;
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

        public static async Task<string> RemovePath(PavlovServer server, string path,
            PavlovServerService pavlovServerService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
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
                        DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                            "Could not remove the " + path + "! " + server.Name, LogEventLevel.Fatal,
                            pavlovServerService._notifyService, result);
                    result.answer = update;
                }
                else
                {
                    result.answer = "Everything is fine there is no file to delete!";
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, pavlovServerService._notifyService, e, result, client, clientSftp);
            }
            finally
            {
                client.Disconnect();
                clientSftp.Disconnect();
            }

            if (restart) await SystemDStart(server, pavlovServerService);

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return EndConnection(result);
        }

        public static void ExcpetionHandlingSshSftp(PavlovServer server, IToastifyService notyfService, Exception e,
            ConnectionResult result, SshClient client = null, SftpClient clientSftp = null)
        {
            switch (e)
            {
                case SshAuthenticationException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not Login over ssh!" + server.Name,
                        LogEventLevel.Fatal, notyfService, result);
                    break;
                case SshConnectionException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host over ssh!" + server.Name,
                        LogEventLevel.Fatal, notyfService, result);
                    break;
                case SshOperationTimeoutException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Could not connect to host cause of timeout over ssh!" + server.Name, LogEventLevel.Fatal,
                        notyfService, result);
                    break;
                case SocketException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host!" + server.Name,
                        LogEventLevel.Fatal, notyfService, result);
                    break;
                case InvalidOperationException _:
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        e.Message + " <- most lily this error is from telnet" + server.Name, LogEventLevel.Fatal,
                        notyfService, result);
                    break;
                default:
                {
                    client?.Disconnect();
                    clientSftp?.Disconnect();
                    throw e;
                }
            }
        }

        public static string DoesPathExist(PavlovServer server, string path, IToastifyService notyfService)
        {
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                clientSftp.Connect();
                if (clientSftp.Exists(path))
                {
                    result.Success = true;
                    result.answer = "true";
                }
                else
                {
                    result.answer = "false";
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, notyfService, e, result, null, clientSftp);
            }
            finally
            {
                clientSftp.Disconnect();
            }

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return EndConnection(result);
        }

        public static async Task<ConnectionResult> SShTunnelMultipleCommands(PavlovServer server,
            string[] commands, IToastifyService notyfService)
        {
            var result = StartClient(server, out var client);
            try
            {
                client.Connect();

                if (client.IsConnected)
                {
                    var portToForward = server.TelnetPort + 50;
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
                                        result.MultiAnswer.Add(singleCommandResult);
                                    }
                                }
                                else
                                {
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                        "Send password but did not get Authenticated=1 answer: " + auth,
                                        LogEventLevel.Verbose, notyfService);
                                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                        "Telnet Client could not authenticate ..." + server.Name, LogEventLevel.Fatal,
                                        notyfService, result);
                                }
                            }
                            else
                            {
                                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                    "Telnet Client did not ask for Password ..." + server.Name, LogEventLevel.Fatal,
                                    notyfService, result);
                            }
                        }
                        else
                        {
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Telnet Client could not connect ..." + server.Name, LogEventLevel.Fatal, notyfService,
                                result);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Telnet Client cannot be reached..." + server.Name,
                        LogEventLevel.Fatal, notyfService, result);
                }
            }
            catch (Exception e)
            {
                ExcpetionHandlingSshSftp(server, notyfService, e, result, client);
            }
            finally
            {
                client.Disconnect();
            }

            if (result.MultiAnswer.Count > 1)
            {
                result.answer = string.Join(",", result.MultiAnswer);
                result.answer = "[" + result.answer + "]";
            }
            else if (result.MultiAnswer.Count == 1)
            {
                result.answer = result.MultiAnswer[0];
            }
            else
            {
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("there was no answer" + server.Name,
                    LogEventLevel.Fatal, notyfService, result);
            }

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return result;
        }

        public static ConnectionResult StartClient(PavlovServer server, out SshClient client)
        {
            if (server.ServerServiceState != ServerServiceState.active)
                throw new CommandException("will not do command while server service is inactive!");
            var type = GetAuthType(server);
            var connectionInfo = ConnectionInfoInternal(server, type, out var result);
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

            return EndConnection(result);
        }

        public static ConnectionInfo ConnectionInfoInternal(PavlovServer server, RconService.AuthType type,
            out ConnectionResult result)
        {
            ConnectionInfo connectionInfo = null;

            result = new ConnectionResult();
            //auth
            if (type == RconService.AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile(new MemoryStream(server.SshServer.SshKeyFileName))};
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PrivateKeyAuthenticationMethod(server.SshServer.SshUsername, keyFiles));
            }
            else if (type == RconService.AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PasswordAuthenticationMethod(server.SshServer.SshUsername, server.SshServer.SshPassword));
            }
            else if (type == RconService.AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[]
                    {new PrivateKeyFile(new MemoryStream(server.SshServer.SshKeyFileName), server.SshServer.SshPassphrase)};
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PasswordAuthenticationMethod(server.SshServer.SshUsername, server.SshServer.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(server.SshServer.SshUsername, keyFiles));
            }

            return connectionInfo;
        }

        public static string GetFile(PavlovServer server, string path, IToastifyService notyfService)
        {
            var connectionResult = new ConnectionResult();
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
                        connectionResult.Success = true;
                        connectionResult.answer = "File is empty";
                        sftp.Disconnect();
                        return EndConnection(connectionResult);
                    }

                    //Download file
                    using (Stream fileStream = outPutStream)
                    {
                        sftp.DownloadFile(path, fileStream);
                    }
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case SshConnectionException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not Login over ssh! " + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SftpPathNotFoundException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Could not find file! (" + path + ") on the server: " + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SftpPermissionDeniedException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Permissions denied for file: (" + path + ") on the server: " +
                                server.Name, LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SshException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host!" + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        default:
                        {
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        }
                    }
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);
                connectionResult.Success = true;
                connectionResult.answer = fileContent;
            }
            finally
            {
                sftp.Disconnect();
            }


            return EndConnection(connectionResult);
        }

        public static string EndConnection(ConnectionResult connectionResult)
        {
            if (!connectionResult.Success)
            {
                if (connectionResult.errors.Count <= 0) throw new CommandException("Could not connect to server!");
                throw new CommandException(Strings.Join(connectionResult.errors.ToArray(), "\n"));
            }

            return connectionResult.answer;
        }

        public static string WriteFile(PavlovServer server, string path, string content, IToastifyService notyfService)
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
                        connectionResult.errors.Add("Can not write file when the parent folder does not exist!");
                        return EndConnection(connectionResult);
                    }

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("create file", LogEventLevel.Verbose, notyfService);
                    if (!sftp.Exists(path)) sftp.Create(path);

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("fill the file", LogEventLevel.Verbose,
                        notyfService);
                    using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
                    {
                        sftp.UploadFile(fileStream, path);
                    }


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
                catch (Exception e)
                {
                    switch (e)
                    {
                        case SshConnectionException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not Login over ssh! " + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SftpPathNotFoundException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Could not find file! (" + path + ") on the server: " + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SftpPermissionDeniedException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                                "Permissions denied for file: (" + path + ") on the server: " +
                                server.Name, LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        case SshException _:
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not connect to host!" + server.Name,
                                LogEventLevel.Fatal, notyfService, result);
                            sftp.Disconnect();
                            return EndConnection(connectionResult);
                        default:
                        {
                            sftp.Disconnect();
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Fatal,
                                notyfService, result);
                            return EndConnection(connectionResult);
                        }
                    }
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace(Environment.NewLine, "") == content.Replace(Environment.NewLine, ""))
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Upload complet finished. also checked and its the same", LogEventLevel.Verbose, notyfService);
                    connectionResult.Success = true;
                    connectionResult.answer = "File upload successfully " + server.Name;
                }
                else
                {
                    connectionResult.Success = false;
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "File in not the same as uploaded. So upload failed! " + server.Name, LogEventLevel.Fatal,
                        notyfService, connectionResult);
                }
            }
            finally
            {
                sftp.Disconnect();
            }

            return EndConnection(connectionResult);
        }


        private static void DeleteDirectory(SftpClient client, string path)
        {
            foreach (var file in client.ListDirectory(path))
                if (file.Name != "." && file.Name != "..")
                {
                    if (file.IsDirectory)
                        DeleteDirectory(client, file.FullName);
                    else
                        client.DeleteFile(file.FullName);
                }

            client.DeleteDirectory(path);
        }
    }
}