using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        
        public RconService(ServerSelectedMapService serverSelectedMapService,SshServerSerivce sshServerSerivce)
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

        private async Task<ConnectionResult> SShTunnel(PavlovServer server, AuthType type, string command,SshServer sshServer)
        {
            var connectionInfo = ConnectionInfo(server, type, out var result,sshServer);
            var guid = Guid.NewGuid();
            var tmpFolderRemote = "/tmp/pavlovNetcatRconWebServer/";
            var pavlovLocalScriptPath = "Temp/pavlovNetcatRconWebServerScript" + guid + ".sh";
            File.Copy("pavlovNetcatRconWebServerScript.sh", pavlovLocalScriptPath, true);
            var pavlovRemoteScriptPath = tmpFolderRemote + "Script" + guid + ".sh";
            var commandFilelocal = "Temp/Command" + guid;
            File.Copy("Command", commandFilelocal, true);
            var commandFileRemote = tmpFolderRemote + "Commands" + guid;
            try
            {
                //connection
                using var client = new SshClient(connectionInfo);
                client.Connect();
                //check if first scripts exist
                using (var sftp = new SftpClient(connectionInfo))
                {
                    try
                    {
                        sftp.Connect();

                        if (sftp.Exists(tmpFolderRemote))
                        {
                            var files = sftp.ListDirectory(tmpFolderRemote);
                            foreach (var file in files.Where(x => x.Name != "." && x.Name != ".."))
                            {
                                var chmodCommandFiles = client.CreateCommand("chmod 7777 " + pavlovRemoteScriptPath);
                                chmodCommandFiles.Execute();
                                sftp.DeleteFile(file.FullName);
                            }

                            var chmodCommandFolder = client.CreateCommand("chmod 7777 " + tmpFolderRemote);
                            chmodCommandFolder.Execute();
                            sftp.DeleteDirectory(tmpFolderRemote);
                        }

                        //sftp clear old files
                        sftp.CreateDirectory(tmpFolderRemote);

                        string text = await File.ReadAllTextAsync(pavlovLocalScriptPath);
                        text = text.Replace("{port}", server.TelnetPort.ToString());
                        await File.WriteAllTextAsync(pavlovLocalScriptPath, text);
                        await File.WriteAllTextAsync(commandFilelocal,
                            server.TelnetPassword + "\n" + command + "\n" + "Disconnect");


                        await using (var uplfileStream = File.OpenRead(pavlovLocalScriptPath))
                        {
                            sftp.UploadFile(uplfileStream, pavlovRemoteScriptPath, true);
                        }

                        await using (var uplfileStream = File.OpenRead(commandFilelocal))
                        {
                            sftp.UploadFile(uplfileStream, commandFileRemote, true);
                        }

                        File.Delete(commandFilelocal);
                        File.Delete(pavlovLocalScriptPath);
                    }
                    finally
                    {
                        sftp.Disconnect();
                    }
                }


                var sshCommand = client.CreateCommand("chmod +x " + pavlovRemoteScriptPath);
                sshCommand.Execute();

                var sshCommandExecuteBtach = client.CreateCommand(pavlovRemoteScriptPath + " " + commandFileRemote);
                sshCommandExecuteBtach.CommandTimeout = TimeSpan.FromMilliseconds(500);
                try
                {
                    sshCommandExecuteBtach.Execute();
                }
                catch (SshOperationTimeoutException)
                {
                    if (!string.IsNullOrEmpty(sshCommandExecuteBtach.Error))
                        result.errors.Add(sshCommandExecuteBtach.Error);
                }

                if (!sshCommandExecuteBtach.Result.Contains("Password:"))
                {
                    result.errors.Add(
                        "After the ssh connection the telnet connection gives strange answers. Can not send command!");
                }

                if (!sshCommandExecuteBtach.Result.Contains("Authenticated=1"))
                {
                    result.errors.Add(
                        "After the ssh connection the telnet connection can not login. Can not send command!");
                }

                Task.Delay(150).Wait();
                // check answer
                result.answer = sshCommandExecuteBtach.Result;

                if (result.errors.Count > 0 || result.answer == "")
                    return result;

                result.Seccuess = true;
                if (result.answer.Contains("{"))
                    result.answer = result.answer.Substring(result.answer.IndexOf("{", StringComparison.Ordinal));
                if (result.answer.StartsWith("Password: Authenticated=1"))
                    result.answer = result.answer.Replace("Password: Authenticated=1", "");
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
                        throw;
                }
                
                return result;
            }

            return result;
        }

        private static ConnectionInfo ConnectionInfo(PavlovServer server, AuthType type, out ConnectionResult result,SshServer sshServer)
        {
            ConnectionInfo connectionInfo = null;

            result = new ConnectionResult();
            //auth
            if (type == AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + sshServer.SshKeyFileName)};
                connectionInfo = new ConnectionInfo(sshServer.Adress,sshServer.SshPort, sshServer.SshUsername,
                    new PrivateKeyAuthenticationMethod(sshServer.SshUsername, keyFiles));
            }
            else if (type == AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(sshServer.Adress,sshServer.SshPort, sshServer.SshUsername,
                    new PasswordAuthenticationMethod(sshServer.SshUsername, sshServer.SshPassword));
            }
            else if (type == AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + sshServer.SshKeyFileName, sshServer.SshPassphrase)};
                connectionInfo = new ConnectionInfo(sshServer.Adress,sshServer.SshPort, sshServer.SshUsername,
                    new PasswordAuthenticationMethod(sshServer.SshUsername, sshServer.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(sshServer.SshUsername, keyFiles));
            }

            return connectionInfo;
        }

        private async Task<ConnectionResult> GetFile(PavlovServer server, AuthType type,string path,SshServer sshServer)
        {
            var ConnectionResult = new ConnectionResult();
            var connectionInfo = ConnectionInfo(server, type, out var result,sshServer);
            using var client = new SshClient(connectionInfo);
            client.Connect();
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
                        throw new CommandException("Could not create file: "+path+" "+e.Message);
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
                ConnectionResult.Seccuess = true;
                ConnectionResult.answer = fileContent;

            }
            finally
            {
                sftp.Disconnect();
            }
            
            return ConnectionResult;
            
        }

        private async Task<ConnectionResult> WriteFile(PavlovServer server, AuthType type, string path,
            SshServer sshServer,string content)
        {

            var ConnectionResult = new ConnectionResult();
            var connectionInfo = ConnectionInfo(server, type, out var result, sshServer);
            using var client = new SshClient(connectionInfo);
            client.Connect();
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
                    ConnectionResult.Seccuess = true;
                    ConnectionResult.answer = "File upload successfully";
                }
                else
                {
                    ConnectionResult.Seccuess = false;
                    ConnectionResult.answer = "File in not the same as uploaded. So upload failed!";
                }
    

            }
            finally
            {
                sftp.Disconnect();
            }

            return ConnectionResult;
        }
        
        public async Task<bool> SaveBlackListEntry(PavlovServer server, List<ServerBans> NewBlackListContent)
        {
            var blacklistArray = NewBlackListContent.Select(x => x.SteamId).ToArray();
            var content = string.Join(Environment.NewLine, blacklistArray);
            var blacklist = await SendCommand(server, server.ServerFolderPath + FilePaths.BanList, false, false,content,true);

            return true;
        }       
        
        public async Task<PlayerModelExtended> GetPlayerInfo(PavlovServer server, string steamId,string username)
        {
            var playerInfo = await SendCommand(server, "InspectPlayer " + steamId);
            var singlePlayer = JsonConvert.DeserializeObject<PlayerModelExtendedRconModel>(playerInfo);
            singlePlayer.PlayerInfo.Username = username;
            return singlePlayer.PlayerInfo;
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
                if(string.IsNullOrEmpty(line)) continue;
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
        private async Task<ConnectionResult> DeleteUnusedMaps(PavlovServer server, AuthType type,SshServer sshServer)
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
            var ConnectionResult = new ConnectionResult();
            var connectionInfo = ConnectionInfo(server, type, out var result,sshServer);
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
                var maps = sftp.ListDirectory("/tmp/workshop/"+ server.ServerPort+"/content/555160");
                foreach (var map in maps)
                {
                    if (!map.IsDirectory) continue;
                    if (map.Name == ".") continue;
                    if (map.Name == "..") continue;
                    if (await _serverSelectedMapService.FindSelectedMap(server.Id, map.Name) != null) // map is on the selectet list
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
                         attributes = sftp.GetAttributes(map.FullName+"/LinuxServer.pak");
                    }
                    catch (SftpPathNotFoundException e)
                    {
                        continue;
                    }
                    var lastAccessTime = attributes.LastAccessTime;
                    if (lastAccessTime < DateTime.Now.Subtract(new TimeSpan(server.DeletAfter,0,0,0)))
                    {
                        var deleteMapCommand = client.CreateCommand("rm -rf " + map.FullName);
                        deleteMapCommand.CommandTimeout = TimeSpan.FromMilliseconds(500);
                        toDeleteMaps.Add(map.Name);
                        var deleteMapsCommandResponse = deleteMapCommand.Execute();
                         if (deleteMapsCommandResponse.Contains("remove write-protected") ||
                             deleteMapsCommandResponse.Contains("Permission denied"))
                         {
                             throw new CommandException("You don't have the rights to delete the maps!");
                         }
                         if (sftp.Exists(map.FullName))
                         {
                             throw new CommandException("Could not delete map!");
                         }
                    }
                    
                    

                    
            
                }
            
            }
            finally
            {
                sftp.Disconnect();
            }
            
            ConnectionResult.Seccuess = true;
            return ConnectionResult;
        }

        //Use every type of auth as a backup way to get the result
        // that can cause long waiting times but i think its better than just do one thing.
        public async Task<string> SendCommand(PavlovServer server, string command, bool deleteUnusedMaps = false,
            bool getFile = false, string writeContent = "",bool writeFile =false)
        {
            var connectionResult = new ConnectionResult();
            
            if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) && File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshServer.SshUsername))
            {
                if (deleteUnusedMaps) connectionResult = await DeleteUnusedMaps(server, AuthType.PrivateKeyPassphrase,server.SshServer);
                else if (getFile) connectionResult = await GetFile(server, AuthType.PrivateKeyPassphrase,command,server.SshServer);
                else if (writeFile) connectionResult = await WriteFile(server, AuthType.PrivateKeyPassphrase,command,server.SshServer,writeContent);
                else connectionResult = await SShTunnel(server, AuthType.PrivateKeyPassphrase, command,server.SshServer);
            }

            if (!connectionResult.Seccuess && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) && !string.IsNullOrEmpty(server.SshServer.SshUsername))
            {
                if (deleteUnusedMaps) connectionResult = await DeleteUnusedMaps(server, AuthType.PrivateKey,server.SshServer);
                else if (getFile) connectionResult = await GetFile(server, AuthType.PrivateKey,command,server.SshServer);
                else if (writeFile) connectionResult = await WriteFile(server, AuthType.PrivateKey,command,server.SshServer,writeContent);
                else connectionResult = await SShTunnel(server, AuthType.PrivateKey, command,server.SshServer);
            }

            if (!connectionResult.Seccuess && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                !string.IsNullOrEmpty(server.SshServer.SshPassword))
            {
                if (deleteUnusedMaps) connectionResult = await DeleteUnusedMaps(server, AuthType.UserPass,server.SshServer);
                else if (getFile) connectionResult = await GetFile(server, AuthType.UserPass,command,server.SshServer);
                else if (writeFile) connectionResult = await WriteFile(server, AuthType.UserPass,command,server.SshServer,writeContent);
                else connectionResult = await SShTunnel(server, AuthType.UserPass, command,server.SshServer);
            }

            if (!connectionResult.Seccuess)
            {
                if(connectionResult.errors.Count<=0) throw new CommandException("Could not connect to server!");
                throw new CommandException(Strings.Join(connectionResult.errors.ToArray(), "\n"));
            }

            return connectionResult.answer;
        }
    }
}