using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PrimS.Telnet;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PavlovRconWebserver.Services
{
    public class RconService
    {
        private enum AuthType
        {
            PrivateKey,
            UserPass,
            PrivateKeyPassphrase
        }

        private async Task<ConnectionResult> SShTunnel(RconServer server, AuthType type, string command)
        {
            ConnectionInfo connectionInfo = null;

            var result = new ConnectionResult();
            //auth
            if (type == AuthType.PrivateKey)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + server.SshKeyFileName)};
                connectionInfo = new ConnectionInfo(server.Adress, server.SshUsername,
                    new PrivateKeyAuthenticationMethod(server.SshUsername, keyFiles));
            }
            else if (type == AuthType.UserPass)
            {
                connectionInfo = new ConnectionInfo(server.Adress, server.SshUsername,
                    new PasswordAuthenticationMethod(server.SshUsername, server.SshPassword));
            }
            else if (type == AuthType.PrivateKeyPassphrase)
            {
                var keyFiles = new[] {new PrivateKeyFile("KeyFiles/" + server.SshKeyFileName, server.SshPassphrase)};
                connectionInfo = new ConnectionInfo(server.Adress, server.SshUsername,
                    new PasswordAuthenticationMethod(server.SshUsername, server.SshPassphrase),
                    new PrivateKeyAuthenticationMethod(server.SshUsername, keyFiles));
            }

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

                        //That part means that it will not work if more than one requets happen?
                        string text = await File.ReadAllTextAsync(pavlovLocalScriptPath);
                        text = text.Replace("{port}", server.TelnetPort.ToString());
                        await File.WriteAllTextAsync(pavlovLocalScriptPath, text);
                        await File.WriteAllTextAsync(commandFilelocal,
                            server.Password + "\n" + command + "\n" + "Disconnect");


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
                //var sshCommand = client.CreateCommand("telnet localhost " + server.TelnetPort);
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

                Task.Delay(500).Wait();
                // check answer
                result.answer = sshCommandExecuteBtach.Result;

                if (result.errors.Count > 0 && result.answer == "")
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

        //Use every type of auth as a backupway to get the result
        // that can cause long waiting times but i think its better than just do one thing.
        //Todo: a system to choose if the user wants it to run this way ore only one specifig type of auth
        public async Task<string> SendCommand(RconServer server, string command)
        {
            var connectionResult = new ConnectionResult();
            if(!server.UseSsh && !server.UseTelnet)
                throw new CommandException("There was no connection type set. please choose one (Telnet/SSH)");
            
            if (server.UseSsh && !string.IsNullOrEmpty(server.SshPassphrase) &&
                !string.IsNullOrEmpty(server.SshKeyFileName) && File.Exists("KeyFiles/" + server.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshUsername))
            {
                connectionResult = await SShTunnel(server, AuthType.PrivateKeyPassphrase, command);
            }

            if (!connectionResult.Seccuess && server.UseSsh && !string.IsNullOrEmpty(server.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshKeyFileName) && !string.IsNullOrEmpty(server.SshUsername))
            {
                connectionResult = await SShTunnel(server, AuthType.PrivateKey, command);
            }

            if (!connectionResult.Seccuess && server.UseSsh && !string.IsNullOrEmpty(server.SshUsername) &&
                !string.IsNullOrEmpty(server.SshPassword))
            {
                connectionResult = await SShTunnel(server, AuthType.UserPass, command);
            }

            if (!connectionResult.Seccuess && server.UseTelnet)
            {
                connectionResult = await SendCommandTelnet(server, command);
            }

            if (!connectionResult.Seccuess)
            {
                throw new CommandException(Strings.Join(connectionResult.errors.ToArray(), "\n"));
            }

            return connectionResult.answer;
        }

        private async Task<ConnectionResult> SendCommandTelnet(RconServer server, string command)
        {
            var result = new ConnectionResult();

            try
            {
                using Client client = new Client(server.Adress, server.TelnetPort,new System.Threading.CancellationToken());
                if (client.IsConnected)
                {
                    Task.Delay(300).Wait();
                    //Say hello
                    var hello = await client.ReadAsync();
                    //Check answer
                    if (!hello.StartsWith("Password:"))
                    {
                        result.errors.Add("There server " + server.Adress + " give stranges answers: " + hello);
                        return result;
                    }


                    // send password
                    await client.WriteLine(server.Password);

                    Task.Delay(300).Wait();
                    // check answer
                    var loginIn = await client.ReadAsync();
                    if (!loginIn.StartsWith("Authenticated=1"))
                    {
                        result.errors.Add("Could not login to server: " + server.Adress);
                    }
                    Task.Delay(300).Wait();
                    // send command
                    await client.WriteLine(command);
                    // check answer
                    result.answer = await client.ReadAsync();

                    Task.Delay(300).Wait();
                    // send Disconnect
                    await client.WriteLine("Disconnect");
                    client.Dispose();
                }

                if (result.errors.Count > 0 && result.answer == "")
                    return result;

                result.Seccuess = true;
                return result;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case InvalidOperationException _:
                        result.errors.Add("Could not connect to host over telnet!");
                        break;
                    default:
                        throw;
                }

                return result;
            }
        }
    }
}