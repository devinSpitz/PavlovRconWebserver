using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PrimS.Telnet;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {
        public enum AuthType
        {
            PrivateKey,
            UserPass,
            PrivateKeyPassphrase
        }

        private readonly MapsService _mapsService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;

        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SteamIdentityService _steamIdentityService;

        public RconService(SteamIdentityService steamIdentityService, ServerSelectedMapService serverSelectedMapService,
            MapsService mapsService, PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService)
        {
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _steamIdentityService = steamIdentityService;
        }

        public static async Task<string> SendCommandForShell(string customCmd, ShellStream stream,
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

        public static async Task<StringBuilder> ReadStream(StreamReader reader)
        {
            var result = new StringBuilder();

            string line;
            while ((line = await reader.ReadLineAsync()) != null) result.AppendLine(line);

            return result;
        }


        public static async Task<AuthType> GetAuthType(PavlovServer server)
        {
            var auths = new List<AuthType>
            {
                AuthType.PrivateKeyPassphrase,
                AuthType.PrivateKey,
                AuthType.UserPass
            };
            foreach (var type in auths)
                try
                {
                    var connectionInfo = ConnectionInfo(server, type, out var result);
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

        public async Task<string> SystemDCheckState(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);


                var state = await SendCommandForShell(
                    "systemctl list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                    stream, @".*(enabled|disabled).*");
                if (state == null)
                {
                    result.errors.Add("Service does not exist!" + server.Name);
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
                            "systemctl is-active " + server.ServerSystemdServiceName + ".service", stream,
                            @"^(?!.*is-active).*active.*$");
                        if (active == null || active.Contains("inactive"))
                            result.answer = "inactive";
                        else
                            result.answer = "active";
                    }
                    else
                    {
                        result.answer = "notAvailable";
                        result.errors.Add("Service does not exist cause he is not enabled and not disabled!" +
                                          server.Name);
                    }
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;
            return EndConnection(result);
        }

        public async Task<ConnectionResult> SystemDStart(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                var disabled = await SendCommandForShell(
                    "systemctl list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                    stream, @".*disabled.*");
                if (disabled != null)
                {
                    var enable = await SendCommandForShell(
                        "systemctl enable " + server.ServerSystemdServiceName + ".service", stream, null);
                    if (enable == null)
                    {
                        result.errors.Add("Could not enable service " + server.Name);
                    }
                    else if (enable.ToLower().Contains("password"))
                    {
                        var enteredPassword = await SendCommandForShell(
                            server.SshServer.SshPassword, stream, @".*[pP]assword.*");
                        if (enteredPassword == "\r\n" || enteredPassword == null)
                            result.errors.Add("Could not disable service after entering the password again!");
                        if (enteredPassword != null && enteredPassword.ToLower().Contains("password"))
                        {
                            var enteredPasswordReload = await SendCommandForShell(
                                server.SshServer.SshPassword, stream, null);
                            if (enteredPasswordReload == "\r\n" || enteredPasswordReload == null)
                                result.errors.Add("Could not disable service after entering the password again!");
                        }
                    }

                    //enable for reload
                }

                var start = await SendCommandForShell(
                    "systemctl restart " + server.ServerSystemdServiceName + ".service", stream, null);
                if (start == null)
                {
                    result.errors.Add("Could not start service " + server.Name);
                }
                else if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                        result.errors.Add("Could not start service after entering the password again!");
                }
                else
                {
                    Console.WriteLine(start);
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            if (result.errors.Count <= 0) result.Success = true;

            return result;
        }

        public async Task<ConnectionResult> SystemDStop(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);
                var disabled = await SendCommandForShell(
                    "systemctl list-unit-files --type service | grep " + server.ServerSystemdServiceName + ".service",
                    stream, @".*enabled.*");
                if (disabled != null)
                {
                    var enable = await SendCommandForShell(
                        "systemctl disable " + server.ServerSystemdServiceName + ".service", stream,
                        @".*[pP]assword.*");
                    if (enable == null)
                    {
                        result.errors.Add("Could not disable service " + server.Name);
                    }
                    else if (enable.ToLower().Contains("password"))
                    {
                        var enteredPassword = await SendCommandForShell(
                            server.SshServer.SshPassword, stream, @".*[pP]assword.*");
                        if (enteredPassword == "\r\n" || enteredPassword == null)
                            result.errors.Add("Could not disable service after entering the password again!");
                        if (enteredPassword != null && enteredPassword.ToLower().Contains("password"))
                        {
                            var enteredPasswordReload = await SendCommandForShell(
                                server.SshServer.SshPassword, stream, null);
                            if (enteredPasswordReload == "\r\n" || enteredPasswordReload == null)
                                result.errors.Add("Could not disable service after entering the password again!");
                        }
                    }
                }

                var start = await SendCommandForShell("systemctl stop " + server.ServerSystemdServiceName + ".service",
                    stream, null);
                if (start == null)
                {
                    result.errors.Add("Could not stop service " + server.Name);
                }
                else if (start.ToLower().Contains("password"))
                {
                    var enteredPassword = await SendCommandForShell(
                        server.SshServer.SshPassword, stream, null);
                    if (enteredPassword == "\r\n" || enteredPassword == null)
                        result.errors.Add("Could not stop service after entering the password again!");
                }
                else
                {
                    Console.WriteLine(start);
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            if (result.errors.Count <= 0) result.Success = true;

            return result;
        }

        

        public async Task<string> UpdateInstallPavlovServer(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                result.Success = false;
                result.errors.Add(" Steam is now enabled on this server!");
                return EndConnection(result);
            }

            var restart = false;
            if (server.ServerServiceState == ServerServiceState.active)
            {
                restart = true;
                await SystemDStop(server);
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
                if (update == null) result.errors.Add("Could not update the pavlovserver " + server.Name);
                result.answer = update;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            if (restart) await SystemDStart(server);

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;


            return EndConnection(result);
        }

        public async Task<string> InstallPavlovServerService(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            if (!server.SshServer.SteamIsAvailable)
            {
                result.Success = false;
                result.errors.Add(" Steam is now enabled on this server!");
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
                var path = "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service";
                //check if file exist
                if (sftp.Exists(path)) sftp.DeleteFile(path);

                using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(serviceTempalte)))
                {
                    sftp.UploadFile(fileStream, path);
                }


                //Download file again to valid result
                var outPutStream = new MemoryStream();
                using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace(Environment.NewLine, "") != serviceTempalte.Replace(Environment.NewLine, ""))
                {
                    result.Success = false;
                    result.errors.Add("Could not upload service file!");
                    return EndConnection(result);
                }

                //daemon reload
                client.Connect();
                var stream =
                    client.CreateShellStream("pavlovRconWebserverSShTunnelSystemdCheck", 80, 24, 800, 600, 1024);


                var state = await SendCommandForShell("systemctl daemon-reload", stream, null);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        sftp.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
                sftp.Disconnect();
            }


            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return EndConnection(result);
        }
        public async Task<string> RemovePath(PavlovServer server,string path)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            var restart = false;
            if (server.ServerServiceState == ServerServiceState.active)
            {
                restart = true;
                await SystemDStop(server);
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
                        "rm -rf "+path, stream, null);
                    if (update == null) result.errors.Add("Could not remove the "+path+"! " + server.Name);
                    result.answer = update;
                }
                else
                {
                    result.answer = "Everything is fine there is no file to delete!";
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        clientSftp.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
                clientSftp.Disconnect();
            }
        
            if (restart)
            {
                await SystemDStart(server);
            }
            
            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;
        
            return EndConnection(result);
        }
        public async Task<string> DoesPathExist(PavlovServer server,string path)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
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
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        clientSftp.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                clientSftp.Disconnect();
            }
            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;
        
            return EndConnection(result);
        }

        private async Task<ConnectionResult> SShTunnelMultipleCommands(PavlovServer server,
            string[] commands)
        {
            if (server.ServerServiceState != ServerServiceState.active)
                throw new CommandException("will not do command while server service is inactive!");
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            using var client = new SshClient(connectionInfo);
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
                            var password = await client2.ReadAsync(TimeSpan.FromMilliseconds(500));
                            Console.WriteLine("Answer: " + password);
                            if (password.Contains("Password"))
                            {
                                await client2.WriteLine(server.TelnetPassword);
                                var auth = await client2.ReadAsync(TimeSpan.FromMilliseconds(500));
                                if (auth.Contains("Authenticated=1"))
                                    foreach (var command in commands)
                                    {
                                        var singleCommandResult = await SingleCommandResult(client2, command);

                                        result.MultiAnswer.Add(singleCommandResult);
                                    }
                                else
                                    result.errors.Add("Telnet Client could not authenticate ..." + server.Name);
                            }
                            else
                            {
                                result.errors.Add("Telnet Client did not ask for Password ..." + server.Name);
                            }
                        }
                        else
                        {
                            result.errors.Add("Telnet Client could not connect ..." + server.Name);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    result.errors.Add("Telnet Client cannot be reached..." + server.Name);
                    Console.WriteLine("Telnet Client cannot be reached..." + server.Name);
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
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
                result.errors.Add("there was no answer" + server.Name);
            }

            if (result.errors.Count <= 0 || result.answer != "") result.Success = true;

            return result;
        }

        public static async Task<string> SingleCommandResult(Client client2, string command)
        {
            await client2.WriteLine(command);
            var commandResult = await client2.ReadAsync(TimeSpan.FromMilliseconds(300));


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

        public async Task<string> SendCommandSShTunnel(PavlovServer server, string command)
        {
            var result = await SShTunnelMultipleCommands(server, new[] {command});

            return EndConnection(result);
        }

        public static ConnectionInfo ConnectionInfo(PavlovServer server, AuthType type, out ConnectionResult result)
        {
            ConnectionInfo connectionInfo = null;

            result = new ConnectionResult();
            //auth
            if (type == AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + server.SshServer.SshKeyFileName)};
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PrivateKeyAuthenticationMethod(server.SshServer.SshUsername, keyFiles));
            }
            else if (type == AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PasswordAuthenticationMethod(server.SshServer.SshUsername, server.SshServer.SshPassword));
            }
            else if (type == AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[]
                    {new PrivateKeyFile("KeyFiles/" + server.SshServer.SshKeyFileName, server.SshServer.SshPassphrase)};
                connectionInfo = new ConnectionInfo(server.SshServer.Adress, server.SshServer.SshPort,
                    server.SshServer.SshUsername,
                    new PasswordAuthenticationMethod(server.SshServer.SshUsername, server.SshServer.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(server.SshServer.SshUsername, keyFiles));
            }

            return connectionInfo;
        }

        public async Task<string> GetFile(PavlovServer server, string path)
        {
            var connectionResult = new ConnectionResult();
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();

                //check if file exist
                if (!sftp.Exists(path))
                    try
                    {
                        sftp.Create(path);
                    }
                    catch (SftpPermissionDeniedException e)
                    {
                        sftp.Disconnect();
                        throw new CommandException(server.Name + ": Could not create file: " + path + " " + e.Message);
                    }

                //Download file
                var outPutStream = new MemoryStream();
                using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
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

        private string EndConnection(ConnectionResult connectionResult)
        {
            if (!connectionResult.Success)
            {
                if (connectionResult.errors.Count <= 0) throw new CommandException("Could not connect to server!");
                throw new CommandException(Strings.Join(connectionResult.errors.ToArray(), "\n"));
            }

            return connectionResult.answer;
        }

        public async Task<string> WriteFile(PavlovServer server, string path, string content)
        {
            var connectionResult = new ConnectionResult();
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            sftp.BufferSize = 4 * 1024; // bypass Payload error large files
            try
            {
                sftp.Connect();
                //check if file exist
                if (sftp.Exists(path)) sftp.DeleteFile(path);

                if (!sftp.Exists(path)) sftp.Create(path);

                using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
                {
                    sftp.UploadFile(fileStream, path);
                }


                //Download file again to valid result
                var outPutStream = new MemoryStream();
                using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = Encoding.Default.GetString(fileContentArray);

                if (fileContent.Replace(Environment.NewLine, "") == content.Replace(Environment.NewLine, ""))
                {
                    connectionResult.Success = true;
                    connectionResult.answer = "File upload successfully " + server.Name;
                }
                else
                {
                    connectionResult.Success = false;
                    connectionResult.answer = "File in not the same as uploaded. So upload failed! " + server.Name;
                }
            }
            finally
            {
                sftp.Disconnect();
            }

            return EndConnection(connectionResult);
        }

        public async Task<bool> SaveBlackListEntry(PavlovServer server, List<ServerBans> NewBlackListContent)
        {
            var blacklistArray = NewBlackListContent.Select(x => x.SteamId).ToArray();
            var content = string.Join(Environment.NewLine, blacklistArray);
            var blacklist = await WriteFile(server, server.ServerFolderPath + FilePaths.BanList, content);
            return true;
        }

        public async Task<string> SShTunnelGetAllInfoFromPavlovServer(PavlovServer server)
        {
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
            using var client = new SshClient(connectionInfo);
            var costumesToSet = new Dictionary<string, string>();
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
                            var password = await client2.ReadAsync(TimeSpan.FromMilliseconds(500));
                            Console.WriteLine("Answer: " + password);
                            if (password.Contains("Password"))
                            {
                                await client2.WriteLine(server.TelnetPassword);
                                var auth = await client2.ReadAsync(TimeSpan.FromMilliseconds(500));
                                if (auth.Contains("Authenticated=1"))
                                {
                                    // it is authetificated
                                    var commandOne = "RefreshList";
                                    var singleCommandResult1 = await SingleCommandResult(client2, commandOne);

                                    var playersList =
                                        JsonConvert.DeserializeObject<PlayerListClass>(singleCommandResult1);
                                    var steamIds = playersList.PlayerList.Select(x => x.UniqueId);
                                    //Inspect PlayerList
                                    var commands = new List<string>();
                                    if (steamIds != null)
                                        foreach (var steamId in steamIds)
                                            commands.Add("InspectPlayer " + steamId);

                                    var playerListRaw = new List<string>();
                                    foreach (var command in commands)
                                    {
                                        var commandResult = await SingleCommandResult(client2, command);
                                        playerListRaw.Add(commandResult);
                                    }

                                    var playerListJson = string.Join(",", playerListRaw);
                                    playerListJson = "[" + playerListJson + "]";
                                    var finsihedPlayerList = new List<PlayerModelExtended>();
                                    var tmpPlayers = JsonConvert.DeserializeObject<List<PlayerModelExtendedRconModel>>(
                                        playerListJson, new JsonSerializerSettings {CheckAdditionalContent = false});
                                    var steamIdentities = await _steamIdentityService.FindAll();

                                    if (tmpPlayers != null)
                                    {
                                        foreach (var player in tmpPlayers)
                                        {
                                            player.PlayerInfo.Username = player.PlayerInfo.PlayerName;
                                            var identity =
                                                steamIdentities?.FirstOrDefault(x =>
                                                    x.Id == player.PlayerInfo.UniqueId);
                                            if (identity != null && (identity.Costume != "None" ||
                                                                     !string.IsNullOrEmpty(identity.Costume)))
                                                costumesToSet.Add(identity.Id, identity.Costume);
                                        }

                                        finsihedPlayerList = tmpPlayers.Select(x => x.PlayerInfo).ToList();
                                    }

                                    var pavlovServerPlayerList = finsihedPlayerList.Select(x => new PavlovServerPlayer
                                    {
                                        Username = x.Username,
                                        UniqueId = x.UniqueId,
                                        KDA = x.KDA,
                                        Cash = x.Cash,
                                        TeamId = x.TeamId,
                                        Score = x.Score,
                                        ServerId = server.Id
                                    }).ToList();
                                    await _pavlovServerPlayerService.Upsert(pavlovServerPlayerList, server.Id);
                                    await _pavlovServerPlayerHistoryService.Upsert(pavlovServerPlayerList.Select(x =>
                                        new PavlovServerPlayerHistory
                                        {
                                            Username = x.Username,
                                            UniqueId = x.UniqueId,
                                            PlayerName = x.PlayerName,
                                            KDA = x.KDA,
                                            Cash = x.Cash,
                                            TeamId = x.TeamId,
                                            Score = x.Score,
                                            ServerId = x.ServerId,
                                            date = DateTime.Now
                                        }).ToList(), server.Id, 1);

                                    var singleCommandResultTwo = await SingleCommandResult(client2, "ServerInfo");
                                    var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(
                                        singleCommandResultTwo.Replace("\"\"", "\"ServerInfo\""));
                                    var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC", ""));
                                    if (map != null)
                                        tmp.ServerInfo.MapPictureLink = map.ImageUrl;


                                    var tmpinfo = new PavlovServerInfo
                                    {
                                        MapLabel = tmp.ServerInfo.MapLabel,
                                        MapPictureLink = tmp.ServerInfo.MapPictureLink,
                                        GameMode = tmp.ServerInfo.GameMode,
                                        ServerName = tmp.ServerInfo.ServerName,
                                        RoundState = tmp.ServerInfo.RoundState,
                                        PlayerCount = tmp.ServerInfo.PlayerCount,
                                        Teams = tmp.ServerInfo.Teams,
                                        Team0Score = tmp.ServerInfo.Team0Score,
                                        Team1Score = tmp.ServerInfo.Team1Score,
                                        ServerId = server.Id
                                    };

                                    await _pavlovServerInfoService.Upsert(tmpinfo);

                                    result.Success = true;


                                    foreach (var customToSet in costumesToSet)
                                        await SendCommandSShTunnel(server,
                                            "SetPlayerSkin " + customToSet.Key + " " + customToSet.Value);
                                }
                                else
                                {
                                    result.errors.Add("Telnet Client could not authenticate ..." + server.Name);
                                }
                            }
                            else
                            {
                                result.errors.Add("Telnet Client did not ask for Password ..." + server.Name);
                            }
                        }
                        else
                        {
                            result.errors.Add("Telnet Client could not connect ..." + server.Name);
                        }

                        client2.Dispose();
                    }

                    client.Disconnect();
                }
                else
                {
                    result.errors.Add("Telnet Client cannot be reached..." + server.Name);
                    Console.WriteLine("Telnet Client cannot be reached..." + server.Name);
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!" + server.Name);
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!" + server.Name);
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!" + server.Name);
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!" + server.Name);
                        break;
                    case InvalidOperationException _:
                        result.errors.Add(e.Message + " <- most lily this error is from telnet" + server.Name);
                        break;
                    default:
                    {
                        client.Disconnect();
                        throw;
                    }
                }
            }
            finally
            {
                client.Disconnect();
            }

            return EndConnection(result);
        }

        public async Task<List<ServerBans>> GetServerBansFromBlackList(PavlovServer server, List<ServerBans> banlist)
        {
            var answer = "";

            try
            {
                answer = await GetFile(server, server.ServerFolderPath + FilePaths.BanList);
            }
            catch (Exception e)
            {
                throw new CommandException(e.Message);
            }

            var lines = answer.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            );
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var split = line.Split("#");
                var tmp = new ServerBans();
                if (split.Length == 1)
                {
                    var dataBaseObject = banlist.FirstOrDefault(x => x.SteamId == split[0]);
                    if (dataBaseObject != null) continue;

                    tmp.SteamId = split[0];
                }

                if (split.Length == 2) tmp.Comment = split[1];

                banlist.Add(tmp);
            }

            return banlist;
        }

        public async Task<ConnectionResult> DeleteUnusedMaps(PavlovServer server)
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
            var type = await GetAuthType(server);
            var connectionInfo = ConnectionInfo(server, type, out var result);
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
                        if (await _serverSelectedMapService.FindSelectedMap(server.Id, map.Name) != null
                        ) // map is on the selectet list
                            continue; // map is selected

                        // Check if map is running
                        var isRunningAnswerCommand = client.CreateCommand("lsof +D " + map.FullName);
                        isRunningAnswerCommand.CommandTimeout = TimeSpan.FromMilliseconds(500);
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

        public static void DeleteDirectory(SftpClient client, string path)
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