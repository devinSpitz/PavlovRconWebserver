using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hangfire;
using LiteDB.Identity.Database;
using Microsoft.VisualBasic;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PavlovRconWebserver.Extensions
{
    public static class RconStatic
    {
        public static async Task CheckBansForAllServers(string connectionString)
        {
            var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce =
                new SshServerSerivce(new LiteDbIdentityContext(connectionString), pavlovServerService);
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerHistoryService =
                new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconSerivce = new RconService(steamIdentityService, serverSelectedMapService, mapsService,
                pavlovServerInfoService, pavlovServerPlayerService,pavlovServerService,sshServerSerivce, pavlovServerPlayerHistoryService);
            var serverBansService = new ServerBansService(new LiteDbIdentityContext(connectionString));
            var servers = await sshServerSerivce.FindAll();
            foreach (var server in servers)
            foreach (var signleServer in server.PavlovServers)
                try
                {
                    var bans = await serverBansService.FindAllFromPavlovServerId(signleServer.Id, true);
                    await rconSerivce.SaveBlackListEntry(signleServer, bans);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
        }

        public static async Task ReloadPlayerListFromServerAndTheServerInfo(string connectionString,
            bool recursive = false)
        {
            var exceptions = new List<Exception>();
            try
            {
                var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
                var serverSelectedMapService =
                    new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
                var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
                var sshServerSerivce =
                    new SshServerSerivce(new LiteDbIdentityContext(connectionString), pavlovServerService);
                var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
                var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString));
                var pavlovServerPlayerService = new PavlovServerPlayerService(
                    new LiteDbIdentityContext(connectionString));
                var pavlovServerPlayerHistoryService =
                    new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
                var rconSerivce = new RconService(steamIdentityService, serverSelectedMapService, mapsService,
                    pavlovServerInfoService, pavlovServerPlayerService,pavlovServerService,sshServerSerivce, pavlovServerPlayerHistoryService);
                var servers = await sshServerSerivce.FindAll();
                foreach (var server in servers)
                foreach (var signleServer in server.PavlovServers.Where(x => x.ServerType == ServerType.Community))
                    try
                    {
                        await rconSerivce.SShTunnelGetAllInfoFromPavlovServer(signleServer);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        Console.WriteLine(e.Message);
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

            BackgroundJob.Schedule(() => ReloadPlayerListFromServerAndTheServerInfo(connectionString, recursive),
                new TimeSpan(0, 1, 0)); // Check for bans and remove them is necessary
        }


        public static async Task<ConnectionResult> StartMatchWithAuth(RconService.AuthType authType,
            PavlovServer server,
            Match match, string connectionString)
        {
            var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var teamSelectedSteamIdentityService =
                new TeamSelectedSteamIdentityService(new LiteDbIdentityContext(connectionString));
            var matchSelectedTeamSteamIdentitiesService =
                new MatchSelectedTeamSteamIdentitiesService(new LiteDbIdentityContext(connectionString));
            var matchSelectedSteamIdentitiesService =
                new MatchSelectedSteamIdentitiesService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerHistoryService =
                new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconService = new RconService(steamIdentityService, serverSelectedMapService, mapsService,
                pavlovServerInfoService, pavlovServerPlayerService,pavlovServerService,sshServerSerivce, pavlovServerPlayerHistoryService);
            var teamService = new TeamService(new LiteDbIdentityContext(connectionString));
            var matchService = new MatchService(new LiteDbIdentityContext(connectionString),
                matchSelectedTeamSteamIdentitiesService, matchSelectedSteamIdentitiesService, pavlovServerService,
                steamIdentityService, teamService, teamSelectedSteamIdentityService);

            var connectionInfo = RconService.ConnectionInfo(server, authType, out var result);
            using var clientSsh = new SshClient(connectionInfo);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                var listOfSteamIdentietiesWhichCanPlay = match.MatchTeam0SelectedSteamIdentities;
                listOfSteamIdentietiesWhichCanPlay.AddRange(match.MatchTeam1SelectedSteamIdentities);
                var list = new List<string>();
                if (listOfSteamIdentietiesWhichCanPlay.Count <= 0 && match.MatchSelectedSteamIdentities.Count <= 0)
                {
                    result.errors.Add("There are no team members so no match will start!");
                    Console.WriteLine("There are no team members so no match will start!");
                    return result;
                }
                else
                {
                    if (match.MatchSelectedSteamIdentities.Count > 0)
                    {
                        list = match.MatchSelectedSteamIdentities.Select(x => Strings.Trim(x.SteamIdentityId+";"+Environment.NewLine)).ToList();
                    }
                    else if (listOfSteamIdentietiesWhichCanPlay.Count > 0)
                    {
                        list = listOfSteamIdentietiesWhichCanPlay.Select(x => Strings.Trim(x.SteamIdentityId)).ToList();
                    }
                   
                }

                //Write whitelist and set server settings

                var whitelist = "";
                try
                {
                    await rconService.WriteFile(server,
                        server.ServerFolderPath + FilePaths.WhiteList,
                        Strings.Join(list.ToArray()));
                    
                }
                catch (Exception e)
                {
                    result.errors.Add("Could not write whitelist for the match! " +
                                      e.Message);
                }

                var serverSettings = new PavlovServerGameIni
                {
                    bEnabled = true,
                    ServerName = match.Name,
                    MaxPlayers = match.PlayerSlots,
                    bSecured = true,
                    bCustomServer = true,
                    bWhitelist = true,
                    RefreshListTime = 120,
                    LimitedAmmoType = 0,
                    TickRate = 90,
                    TimeLimit = match.TimeLimit,
                    Password = null,
                    BalanceTableURL = null,
                    MapRotation = new List<PavlovServerGameIniMap>
                    {
                        new PavlovServerGameIniMap
                        {
                            MapLabel = match.MapId,
                            GameMode = match.GameMode
                        }
                    }
                };
                var map = await mapsService.FindOne(match.MapId.Replace("UGC", ""));
                await serverSettings.SaveToFile(server, new List<ServerSelectedMap>
                {
                    new ServerSelectedMap
                    {
                        Map = map,
                        GameMode = match.GameMode
                    }
                }, rconService);
                await rconService.SystemDStart(server);

                //StartWatchServiceForThisMatch
                match.Status = Status.StartetWaitingForPlayer;
                await matchService.Upsert(match);
                Console.WriteLine("Start backgroundjob");
                BackgroundJob.Schedule(
                    () => MatchInspector(authType, match.Id, connectionString),
                    new TimeSpan(0, 0, 5)); // ChecjServerState
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

        public static async Task MatchInspector(RconService.AuthType authType,
            int matchId, string connectionString)
        {
            var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var teamSelectedSteamIdentityService =
                new TeamSelectedSteamIdentityService(new LiteDbIdentityContext(connectionString));
            var matchSelectedTeamSteamIdentitiesService =
                new MatchSelectedTeamSteamIdentitiesService(new LiteDbIdentityContext(connectionString));
            var matchSelectedSteamIdentitiesService =
                new MatchSelectedSteamIdentitiesService(new LiteDbIdentityContext(connectionString));
            var teamService = new TeamService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString));
            var pavlovServerPlayerHistoryService =
                new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconService = new RconService(steamIdentityService, serverSelectedMapService, mapsService,
                pavlovServerInfoService, pavlovServerPlayerService,pavlovServerService,sshServerSerivce,  pavlovServerPlayerHistoryService);
            var matchService = new MatchService(new LiteDbIdentityContext(connectionString),
                matchSelectedTeamSteamIdentitiesService, matchSelectedSteamIdentitiesService, pavlovServerService,
                steamIdentityService, teamService, teamSelectedSteamIdentityService);
            Console.WriteLine("MatchInspector started!");
            var match = await matchService.FindOne(matchId);

            try
            {
                if (match.ForceSop)
                {
                    Console.WriteLine("Endmatch!");
                    await EndMatch(authType, match.PavlovServer, match, rconService, matchService);
                    return;
                }

                await rconService.SShTunnelGetAllInfoFromPavlovServer(match.PavlovServer);
                switch (match.Status)
                {
                    case Status.StartetWaitingForPlayer:

                        Console.WriteLine("TryToStartMatch started!");
                        await TryToStartMatch(match.PavlovServer, match, rconService, matchService, pavlovServerPlayerService);
                        break;
                    case Status.OnGoing:

                        Console.WriteLine("OnGoing!");
                        var serverInfo = await pavlovServerInfoService.FindServer(match.PavlovServer.Id);
                        match.PlayerResults = (await pavlovServerPlayerService.FindAllFromServer(match.PavlovServer.Id)).ToList();
                        match.EndInfo = serverInfo;
                        await matchService.Upsert(match);

                        if (serverInfo.RoundState == "Ended")
                        {
                            Console.WriteLine("Round ended!");
                            match.PlayerResults =
                                (await pavlovServerPlayerService.FindAllFromServer(match.PavlovServer.Id)).ToList();
                            match.EndInfo = serverInfo;
                            await EndMatch(authType, match.PavlovServer, match, rconService, matchService);
                            return;
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (match.Status != Status.Finshed)
                BackgroundJob.Schedule(
                    () => MatchInspector(authType, match.Id, connectionString),
                    new TimeSpan(0, 0, 5)); // ChecjServerState
        }

        private static async Task EndMatch(RconService.AuthType authType, PavlovServer server, Match match,
            RconService rconService,
            MatchService matchService)
        {
            match.Status = Status.Finshed;
            await matchService.Upsert(match);
            await rconService.SystemDStop(server);
            Console.WriteLine("Stopped server!");
        }

        private static async Task TryToStartMatch(PavlovServer server, Match match, RconService rconService,
            MatchService matchService, PavlovServerPlayerService pavlovServerPlayerService)
        {
            var playerList = (await pavlovServerPlayerService.FindAllFromServer(server.Id)).ToList();
            Console.WriteLine(" playerlistcount = "+playerList.Count());
            Console.WriteLine(" match.PlayerSlots  = "+match.PlayerSlots );
            if (playerList.Count() == match.PlayerSlots || match.ForceStart) //All Player are here
            {
                //Do Players in the right team
                foreach (var pavlovServerPlayer in playerList)
                {
                    var team0 = match.MatchTeam0SelectedSteamIdentities.FirstOrDefault(x =>
                        x.SteamIdentityId == pavlovServerPlayer.UniqueId);
                    var team1 = match.MatchTeam1SelectedSteamIdentities.FirstOrDefault(x =>
                        x.SteamIdentityId == pavlovServerPlayer.UniqueId);
                    if (team0 != null)
                    {
                        Console.WriteLine("SwitchTeam 0 " + pavlovServerPlayer.UniqueId);
                        await SendCommandTillDone(server, rconService, "SwitchTeam 0 " + pavlovServerPlayer.UniqueId);
                    }
                    else if (team1 != null)
                    {
                        Console.WriteLine("SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                        await SendCommandTillDone(server, rconService, "SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                    }
                }
                //All Players are on the right team now
                //ResetSND

                Console.WriteLine("start ResetSND!");
                await SendCommandTillDone(server, rconService, "ResetSND");
                match.Status = Status.OnGoing;
                await matchService.Upsert(match);
            }
        }

        private static async Task<string> SendCommandTillDone(PavlovServer server, RconService rconService,
            string command, int timeoutInSeconds = 60)
        {
            var task = Task.Run(() => SendCommandTillDoneChild(server, rconService, command));
            if (task.Wait(TimeSpan.FromSeconds(timeoutInSeconds)))
                return task.Result;
            throw new Exception("Timed out");
        }

        private static async Task<string> SendCommandTillDoneChild(PavlovServer server, RconService rconService,
            string command)
        {
            while (true)
                try
                {
                    var result = await rconService.SendCommandSShTunnel(server, command);
                    return result;
                }
                catch (CommandException e)
                {
                    Console.WriteLine(e);
                    throw;
                }
        }
    }
}