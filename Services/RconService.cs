using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {

        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SshServerSerivce _sshServerSerivce;

        public RconService(ServerSelectedMapService serverSelectedMapService, SshServerSerivce sshServerSerivce)
        {
            _serverSelectedMapService = serverSelectedMapService;
            _sshServerSerivce = sshServerSerivce;
        }

        public enum AuthType
        {
            PrivateKey,
            UserPass,
            PrivateKeyPassphrase
        }

        private async Task<ConnectionResult> SShTunnelMultipleCommands(PavlovServer server, AuthType type,
            string[] commands,
            SshServer sshServer)
        {
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
            using var client = new SshClient(connectionInfo);
            try
            {
                client.Connect();
                ShellStream stream = client.CreateShellStream("pavlovRconWebserver", 80, 24, 800, 600, 1024);
                var telnetConnectResult = await SendCommandForShell("nc localhost " + server.TelnetPort, stream);
                if (telnetConnectResult.ToString().Contains("Password:"))
                {
                    var authResult = await SendCommandForShell(server.TelnetPassword, stream);
                    if (authResult.ToString().Contains("Authenticated=1"))
                    {
                        foreach (var command in commands)
                        {
                            var commandResult = await SendCommandForShell(command, stream);

                            string singleCommandResult = "";
                            if (commandResult.ToString().Contains("{"))
                            {
                                singleCommandResult = commandResult.ToString()
                                    .Substring(commandResult.ToString().IndexOf("{", StringComparison.Ordinal));
                            }

                            if (singleCommandResult.StartsWith("Password: Authenticated=1"))
                                singleCommandResult = singleCommandResult.Replace("Password: Authenticated=1", "");


                            if (singleCommandResult.Contains(command))
                                singleCommandResult = singleCommandResult.Replace(command, "");

                            result.MultiAnswer.Add(singleCommandResult);

                        }

                    }
                    else
                    {
                        result.errors.Add(
                            "After the ssh connection the telnet connection can not login. Can not send command!");
                    }
                }
                else
                {
                    result.errors.Add(
                        "After the ssh connection the telnet connection gives strange answers. Can not send command!");
                }

                await SendCommandForShell("Disconect", stream);
                stream.Close();

            }catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!");
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!");
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!");
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!");
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

            result.answer = string.Join(",", result.MultiAnswer);
            result.answer = "[" + result.answer + "]";
            if (result.errors.Count <= 0 || result.answer != "")
            {
                result.Seccuess = true;
            }

            return result;

        }

        private async Task<StringBuilder> SendCommandForShell(string customCmd, ShellStream stream)
        {
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            await WriteStream(customCmd, writer, stream);
            Task.Delay(100).Wait();
            var answer = await ReadStream(reader);
            return answer;
        }

        private async Task WriteStream(string cmd, StreamWriter writer, ShellStream stream)
        {
            await writer.WriteLineAsync(cmd);
            while (stream.Length == 0)
            {
                Task.Delay(100).Wait();
            }
        }

        private async Task<StringBuilder> ReadStream(StreamReader reader)
        {
            StringBuilder result = new StringBuilder();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                result.AppendLine(line);
            }

            return result;
        }

        private async Task<ConnectionResult> SShTunnel(PavlovServer server, AuthType type, string command,
            SshServer sshServer)
        {
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
            using var client = new SshClient(connectionInfo);
            //connection
            try
            {
                client.Connect();
                ShellStream stream = client.CreateShellStream("pavlovRconWebserver", 80, 24, 800, 600, 1024);
                var telnetConnectResult = await SendCommandForShell("nc localhost " + server.TelnetPort, stream);
                if (telnetConnectResult.ToString().Contains("Password:"))
                {
                    var authResult = await SendCommandForShell(server.TelnetPassword, stream);
                    if (authResult.ToString().Contains("Authenticated=1"))
                    {
                        var commandResult = await SendCommandForShell(command, stream);

                        string singleCommandResult = "";
                        if (commandResult.ToString().Contains("{"))
                        {
                            singleCommandResult = commandResult.ToString()
                                .Substring(commandResult.ToString().IndexOf("{", StringComparison.Ordinal));
                        }

                        if (singleCommandResult.StartsWith("Password: Authenticated=1"))
                            singleCommandResult = singleCommandResult.Replace("Password: Authenticated=1", "");


                        if (singleCommandResult.Contains(command))
                            singleCommandResult = singleCommandResult.Replace(command, "");

                        await SendCommandForShell("Disconect", stream);
                        result.answer = singleCommandResult;

                    }
                    else
                    {
                        result.errors.Add(
                            "After the ssh connection the telnet connection can not login. Can not send command!");
                    }
                }
                else
                {
                    result.errors.Add(
                        "After the ssh connection the telnet connection gives strange answers. Can not send command!");
                }

                await SendCommandForShell("Disconect", stream);
                stream.Close();
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case SshAuthenticationException _:
                        result.errors.Add("Could not Login over ssh!");
                        break;
                    case SshConnectionException _:
                        result.errors.Add("Could not connect to host over ssh!");
                        break;
                    case SshOperationTimeoutException _:
                        result.errors.Add("Could not connect to host cause of timeout over ssh!");
                        break;
                    case SocketException _:
                        result.errors.Add("Could not connect to host!");
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

            if (result.errors.Count > 0 || result.answer == "")
                return result;

            result.Seccuess = true;
            return result;
        }

        private static ConnectionInfo ConnectionInfo(PavlovServer server, AuthType type, out ConnectionResult result,
            SshServer sshServer)
        {
            ConnectionInfo connectionInfo = null;

            result = new ConnectionResult();
            //auth
            if (type == AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + sshServer.SshKeyFileName)};
                connectionInfo = new ConnectionInfo(sshServer.Adress, sshServer.SshPort, sshServer.SshUsername,
                    new PrivateKeyAuthenticationMethod(sshServer.SshUsername, keyFiles));
            }
            else if (type == AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(sshServer.Adress, sshServer.SshPort, sshServer.SshUsername,
                    new PasswordAuthenticationMethod(sshServer.SshUsername, sshServer.SshPassword));
            }
            else if (type == AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[]
                    {new PrivateKeyFile("KeyFiles/" + sshServer.SshKeyFileName, sshServer.SshPassphrase)};
                connectionInfo = new ConnectionInfo(sshServer.Adress, sshServer.SshPort, sshServer.SshUsername,
                    new PasswordAuthenticationMethod(sshServer.SshUsername, sshServer.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(sshServer.SshUsername, keyFiles));
            }

            return connectionInfo;
        }

        private async Task<ConnectionResult> GetFile(PavlovServer server, AuthType type, string path,
            SshServer sshServer)
        {
            var connectionResult = new ConnectionResult();
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();

                //check if file exist
                if (!sftp.Exists(path))
                {
                    try
                    {
                        sftp.Create(path);
                    }
                    catch (SftpPermissionDeniedException e)
                    {
                        sftp.Disconnect();
                        throw new CommandException("Could not create file: " + path + " " + e.Message);
                    }
                }

                //Download file
                var outPutStream = new MemoryStream();
                using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = System.Text.Encoding.Default.GetString(fileContentArray);
                connectionResult.Seccuess = true;
                connectionResult.answer = fileContent;

            }
            finally
            {
                sftp.Disconnect();
            }

            return connectionResult;

        }

        private async Task<ConnectionResult> WriteFile(PavlovServer server, AuthType type, string path,
            SshServer sshServer, string content)
        {

            var connectionResult = new ConnectionResult();
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
            //check if first scripts exist
            using var sftp = new SftpClient(connectionInfo);
            try
            {
                sftp.Connect();

                //check if file exist
                if (sftp.Exists(path))
                {
                    sftp.DeleteFile(path);
                }


                using (var fileStream = new MemoryStream(Encoding.ASCII.GetBytes(content)))
                {
                    sftp.BufferSize = 4 * 1024; // bypass Payload error large files
                    sftp.UploadFile(fileStream, path);
                }

                //Download file again to valid result
                var outPutStream = new MemoryStream();
                using (Stream fileStream = outPutStream)
                {
                    sftp.DownloadFile(path, fileStream);
                }

                var fileContentArray = outPutStream.ToArray();
                var fileContent = System.Text.Encoding.Default.GetString(fileContentArray);

                if (fileContent == content)
                {
                    connectionResult.Seccuess = true;
                    connectionResult.answer = "File upload successfully";
                }
                else
                {
                    connectionResult.Seccuess = false;
                    connectionResult.answer = "File in not the same as uploaded. So upload failed!";
                }


            }
            finally
            {
                sftp.Disconnect();
            }

            return connectionResult;
        }

        public async Task<bool> SaveBlackListEntry(PavlovServer server, List<ServerBans> NewBlackListContent)
        {
            var blacklistArray = NewBlackListContent.Select(x => x.SteamId).ToArray();
            var content = string.Join(Environment.NewLine, blacklistArray);
            var blacklist = await SendCommand(server, server.ServerFolderPath + FilePaths.BanList, false, false,
                content, true);

            return true;
        }

        public async Task<List<PlayerModelExtended>> GetPlayerInfo(PavlovServer server, List<string> steamIds)
        {
            List<string> commands = new List<string>();
            if (steamIds != null)
            {
                foreach (var steamId in steamIds)
                {
                    commands.Add("InspectPlayer " + steamId);
                }
            }

            var playerInfo = await SendCommand(server, "", false, false, "", false, true, commands);
            var tmpPlayers = JsonConvert.DeserializeObject<List<PlayerModelExtendedRconModel>>(playerInfo,new JsonSerializerSettings{CheckAdditionalContent = false});
            foreach (var player in tmpPlayers)
            {
                player.PlayerInfo.Username = player.PlayerInfo.PlayerName;
            }
            return tmpPlayers.Select(x => x.PlayerInfo).ToList();

        }

        public async Task<List<ServerBans>> GetServerBansFromBlackList(PavlovServer server, List<ServerBans> banlist)
        {
            var blacklist = await SendCommand(server, server.ServerFolderPath + FilePaths.BanList, false, true);
            string[] lines = blacklist.Split(
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
                    if (dataBaseObject != null)
                    {
                        continue;
                    }

                    tmp.SteamId = split[0];
                }

                if (split.Length == 2)
                {
                    tmp.Comment = split[1];
                }

                banlist.Add(tmp);
            }

            return banlist;
        }

        private async Task<ConnectionResult> DeleteUnusedMaps(PavlovServer server, AuthType type, SshServer sshServer)
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
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
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
                var maps = sftp.ListDirectory("/tmp/workshop/" + server.ServerPort + "/content/555160");
                foreach (var map in maps)
                {
                    if (!map.IsDirectory) continue;
                    if (map.Name == ".") continue;
                    if (map.Name == "..") continue;
                    if (await _serverSelectedMapService.FindSelectedMap(server.Id, map.Name) != null
                    ) // map is on the selectet list
                    {
                        continue; // map is selected
                    }

                    // Check if map is running
                    var isRunningAnswerCommand = client.CreateCommand("lsof +D " + map.FullName);
                    isRunningAnswerCommand.CommandTimeout = TimeSpan.FromMilliseconds(500);
                    var isRunningAnswer = isRunningAnswerCommand.Execute();
                    if (isRunningAnswer.Contains("COMMAND") && isRunningAnswer.Contains("USER")
                    ) // map is running on the server
                    {
                        continue; // map is in use
                    }

                    //Check usage
                    SftpFileAttributes attributes = null;
                    try
                    {
                        attributes = sftp.GetAttributes(map.FullName + "/LinuxServer.pak");
                    }
                    catch (SftpPathNotFoundException e)
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
                            throw new CommandException("Permission denied to delet map");
                        }

                        if (sftp.Exists(map.FullName))
                        {
                            sftp.Disconnect();
                            throw new CommandException("Could not delete map!");
                        }
                    }





                }

            }
            finally
            {
                sftp.Disconnect();
            }

            client.Disconnect();
            connectionResult.Seccuess = true;
            return connectionResult;
        }

        private static void DeleteDirectory(SftpClient client, string path)
        {
            foreach (SftpFile file in client.ListDirectory(path))
            {
                if ((file.Name != ".") && (file.Name != ".."))
                {
                    if (file.IsDirectory)
                    {
                        DeleteDirectory(client, file.FullName);
                    }
                    else
                    {
                        client.DeleteFile(file.FullName);
                    }
                }
            }

            client.DeleteDirectory(path);
        }

        //Use every type of auth as a backup way to get the result
        // that can cause long waiting times but i think its better than just do one thing.
        public async Task<string> SendCommand(PavlovServer server, string command, bool deleteUnusedMaps = false,
            bool getFile = false, string writeContent = "", bool writeFile = false, bool multiCommand = false,
            List<string> multiCommands = null)
        {
            var connectionResult = new ConnectionResult();

            if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshServer.SshUsername))
            {
                if (deleteUnusedMaps)
                    connectionResult = await DeleteUnusedMaps(server, AuthType.PrivateKeyPassphrase, server.SshServer);
                else if (getFile)
                    connectionResult = await GetFile(server, AuthType.PrivateKeyPassphrase, command, server.SshServer);
                else if (writeFile)
                    connectionResult = await WriteFile(server, AuthType.PrivateKeyPassphrase, command, server.SshServer,
                        writeContent);
                else if (multiCommand && multiCommands != null)
                    connectionResult = await SShTunnelMultipleCommands(server, AuthType.PrivateKeyPassphrase,
                        multiCommands.ToArray(), server.SshServer);
                else
                    connectionResult =
                        await SShTunnel(server, AuthType.PrivateKeyPassphrase, command, server.SshServer);
            }

            if (!connectionResult.Seccuess && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshServer.SshUsername))
            {
                if (deleteUnusedMaps)
                    connectionResult = await DeleteUnusedMaps(server, AuthType.PrivateKey, server.SshServer);
                else if (getFile)
                    connectionResult = await GetFile(server, AuthType.PrivateKey, command, server.SshServer);
                else if (writeFile)
                    connectionResult = await WriteFile(server, AuthType.PrivateKey, command, server.SshServer,
                        writeContent);
                else if (multiCommand && multiCommands != null)
                    connectionResult = await SShTunnelMultipleCommands(server, AuthType.PrivateKey,
                        multiCommands.ToArray(), server.SshServer);
                else connectionResult = await SShTunnel(server, AuthType.PrivateKey, command, server.SshServer);
            }

            if (!connectionResult.Seccuess && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                !string.IsNullOrEmpty(server.SshServer.SshPassword))
            {
                if (deleteUnusedMaps)
                    connectionResult = await DeleteUnusedMaps(server, AuthType.UserPass, server.SshServer);
                else if (getFile)
                    connectionResult = await GetFile(server, AuthType.UserPass, command, server.SshServer);
                else if (writeFile)
                    connectionResult =
                        await WriteFile(server, AuthType.UserPass, command, server.SshServer, writeContent);
                else if (multiCommand && multiCommands != null)
                    connectionResult = await SShTunnelMultipleCommands(server, AuthType.UserPass,
                        multiCommands.ToArray(), server.SshServer);
                else connectionResult = await SShTunnel(server, AuthType.UserPass, command, server.SshServer);
            }

            if (!connectionResult.Seccuess)
            {
                if (connectionResult.errors.Count <= 0) throw new CommandException("Could not connect to server!");
                throw new CommandException(Strings.Join(connectionResult.errors.ToArray(), "\n"));
            }

            return connectionResult.answer;
        }
    }
}