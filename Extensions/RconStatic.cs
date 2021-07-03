using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire;
using HtmlAgilityPack;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using Renci.SshNet;
using Renci.SshNet.Common;
using Match = PavlovRconWebserver.Models.Match;

namespace PavlovRconWebserver.Extensions
{
    public static class RconStatic
    { 
        
        public static async Task CheckBansForAllServers(string connectionString)
        {
            
            var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,mapsService);
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,pavlovServerInfoService);
            var pavlovServerPlayerHistoryService = new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconSerivce = new RconService(steamIdentityService,serverSelectedMapService,mapsService,pavlovServerInfoService,pavlovServerPlayerService,pavlovServerPlayerHistoryService);
            var serverBansService = new ServerBansService(new LiteDbIdentityContext(connectionString));
            var servers = await sshServerSerivce.FindAll();
            foreach (var server in servers)
            {
                
                foreach (var signleServer in server.PavlovServers)
                {
                    try
                    {
                        var bans = await serverBansService.FindAllFromPavlovServerId(signleServer.Id,true);
                        await rconSerivce.SaveBlackListEntry(signleServer, bans);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    } 
                }
            }
        }        
        
        public static async Task ReloadPlayerListFromServerAndTheServerInfo(string connectionString,bool recursive = false)
        {
            var exceptions = new List<Exception>();
            try
            {
                var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
                var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
                var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
                var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
                var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
                var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,mapsService);
                var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,pavlovServerInfoService);
                var pavlovServerPlayerHistoryService = new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
                var rconSerivce = new RconService(steamIdentityService,serverSelectedMapService,mapsService,pavlovServerInfoService,pavlovServerPlayerService,pavlovServerPlayerHistoryService);
                var servers = await sshServerSerivce.FindAll();
                foreach (var server in servers)
                {
                    foreach (var signleServer in server.PavlovServers.Where(x=>x.ServerType == ServerType.Community))
                    {
                        try
                        {
                            await rconSerivce.SendCommand(signleServer,"",false,false,"",false,false,null,true);
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                            Console.WriteLine(e.Message);
                        } 
                    }
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                Console.WriteLine(e.Message);
            }
            // Ignore them for now
            // if (exceptions.Count > 0)
            // {
            //     throw new Exception(String.Join(" | Next Exception:  ",exceptions.Select(x=>x.Message).ToList()));
            // }

            BackgroundJob.Schedule(() => ReloadPlayerListFromServerAndTheServerInfo(connectionString, recursive),new TimeSpan(0, 1, 0)); // Check for bans and remove them is necessary

        }
        
        
        public static async Task StartMatch(string connectionString,int matchId)
        {
            var exceptions = new List<Exception>();
            try
            {
                var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
                var matchService = new MatchService(new LiteDbIdentityContext(connectionString));
                var match = await matchService.FindOne(matchId);
                var server = await pavlovServerService.FindOne(match.PavlovServer.Id);
                
                var connectionResult = new ConnectionResult();
                 if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshServer.SshUsername))
                 {

                     connectionResult = await StartMatchWithAuth(
                         RconService.AuthType.PrivateKeyPassphrase,server,match);
                }

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                {
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.PrivateKey,server,match);
                }

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                    !string.IsNullOrEmpty(server.SshServer.SshPassword))
                {
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.UserPass,server,match);
                }

                if (!connectionResult.Success)
                {
                    if (connectionResult.errors.Count <= 0) throw new CommandException("Could not connect to server!");
                }


            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            
        }

        public static async Task<ConnectionResult> StartMatchWithAuth(RconService.AuthType authType,PavlovServer server,Match match)
        {
            var connectionInfo = RconService.ConnectionInfo(server, authType, out var result, server.SshServer);
            using var clientSsh = new SshClient(connectionInfo);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                clientSsh.Connect();
                ShellStream stream = clientSsh.CreateShellStream("pavlovRconWebserverSShTunnelMultipleCommands", 80, 24,
                    800, 600, 1024);
                // var telnetConnectResult = await RconService.SendCommandForShell("nc localhost " + server.TelnetPort, stream);
                // if (telnetConnectResult.ToString().Contains("Password:"))
                // {
                //     
                // }
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
                        clientSsh.Disconnect();
                        clientSftp.Disconnect();
                        throw;
                    }
                }

            }
            finally
            {
                clientSsh.Disconnect();
                clientSftp.Disconnect();
            }

            return result;
        }
    }
}