using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using Renci.SshNet;
using Serilog.Events;
using Match = PavlovRconWebserver.Models.Match;

namespace PavlovRconWebserver.Services
{
    public class MatchService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly MapsService _mapsService;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;

        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly IToastifyService _notifyService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly RconService _rconService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly TeamService _teamService;
        private readonly ServerSelectedModsService _serverSelectedModsService;


        public MatchService(ILiteDbIdentityAsyncContext liteDbContext,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentitiesService,
            PavlovServerService pavlovServerService,
            SteamIdentityService steamIdentityService,
            RconService rconService,
            MapsService mapsService,
            TeamService teamService,
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            ServerSelectedModsService serverSelectedModsService,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
            _matchSelectedTeamSteamIdentitiesService = matchSelectedTeamSteamIdentitiesService;
            _matchSelectedSteamIdentitiesService = matchSelectedSteamIdentitiesService;
            _pavlovServerService = pavlovServerService;
            _teamService = teamService;
            _teamSelectedSteamIdentityService = teamSelectedSteamIdentityService;
            _steamIdentityService = steamIdentityService;
            _rconService = rconService;
            _mapsService = mapsService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _serverSelectedModsService = serverSelectedModsService;
        }

        public async Task<bool> SaveMatchToService(MatchViewModel match, Match realmatch)
        {
            realmatch.Name = match.Name;
            realmatch.MapId = match.MapId;
            realmatch.GameMode = match.GameMode;
            realmatch.TimeLimit = match.TimeLimit;
            realmatch.PlayerSlots = match.PlayerSlots;

            var gotAnswer = GameModes.HasTeams.TryGetValue(realmatch.GameMode, out var hasTeams);
            if (gotAnswer)
            {
                if (hasTeams)
                {
                    realmatch.Team0 = await _teamService.FindOne((int) match.Team0Id);
                    realmatch.Team0.TeamSelectedSteamIdentities =
                        (await _teamSelectedSteamIdentityService.FindAllFrom(realmatch.Team0.Id)).ToList();
                    realmatch.Team1 = await _teamService.FindOne((int) match.Team1Id);
                    realmatch.Team1.TeamSelectedSteamIdentities =
                        (await _teamSelectedSteamIdentityService.FindAllFrom(realmatch.Team1.Id)).ToList();

                    // Check all steam identities
                    foreach (var team0SelectedSteamIdentity in match.MatchTeam0SelectedSteamIdentitiesStrings)
                    {
                        var tmp = realmatch.Team0.TeamSelectedSteamIdentities.FirstOrDefault(x =>
                            x.SteamIdentity.Id.ToString() == team0SelectedSteamIdentity);
                        if (tmp != null)
                            realmatch.MatchTeam0SelectedSteamIdentities.Add(new MatchTeamSelectedSteamIdentity
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = team0SelectedSteamIdentity,
                                TeamId = 0,
                                OverWriteRole = tmp.RoleOverwrite
                            });
                        else
                            return true;
                    }

                    foreach (var team1SelectedSteamIdentity in match.MatchTeam1SelectedSteamIdentitiesStrings)
                    {
                        var tmp = realmatch.Team1.TeamSelectedSteamIdentities.FirstOrDefault(x =>
                            x.SteamIdentity.Id.ToString() == team1SelectedSteamIdentity);
                        if (tmp != null)
                            realmatch.MatchTeam1SelectedSteamIdentities.Add(new MatchTeamSelectedSteamIdentity
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = team1SelectedSteamIdentity,
                                TeamId = 1,
                                OverWriteRole = tmp.RoleOverwrite
                            });
                        else
                            return true;
                    }
                }
                else
                {
                    foreach (var SelectedSteamIdentity in match.MatchSelectedSteamIdentitiesStrings)
                    {
                        var tmp = await _steamIdentityService.FindOne(SelectedSteamIdentity);
                        if (tmp != null)
                            realmatch.MatchSelectedSteamIdentities.Add(new MatchSelectedSteamIdentity
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = SelectedSteamIdentity
                            });
                        else
                            return true;
                    }
                }

                //When not a server is set!!!! or server already is running a match
                if (match.PavlovServerId <= 0)
                    return true;
                realmatch.PavlovServer = await _pavlovServerService.FindOne(match.PavlovServerId);
                if (realmatch.PavlovServer == null) return true;
                realmatch.Status = match.Status;
            }
            else
            {
                return true;
            }


            //Problem if i save here the MatchID upper is not set  until its an update:(

            var bla = await Upsert(realmatch);

            if (bla)
            {

                
                if (realmatch.MatchSelectedSteamIdentities.Count > 0)
                {
                    foreach (var matchSelectedSteamIdentity in realmatch.MatchSelectedSteamIdentities)
                    {
                        matchSelectedSteamIdentity.matchId = realmatch.Id;
                    }
                    // First remove Old TeamSelected and Match selected stuff
                    // Then write the new ones
                    await _matchSelectedSteamIdentitiesService.RemoveFromMatch(realmatch.Id);
                    await _matchSelectedSteamIdentitiesService.Upsert(realmatch.MatchSelectedSteamIdentities,match.Id);
                }

                if(realmatch.MatchTeam0SelectedSteamIdentities.Count > 0||realmatch.MatchTeam1SelectedSteamIdentities.Count > 0)
                {
                    await _matchSelectedTeamSteamIdentitiesService.RemoveFromMatch(realmatch.Id);
                    foreach (var matchTeam0SelectedSteamIdentity in realmatch.MatchTeam0SelectedSteamIdentities)
                    {
                        matchTeam0SelectedSteamIdentity.matchId = realmatch.Id;
                    }
                    foreach (var matchTeam1SelectedSteamIdentity in realmatch.MatchTeam1SelectedSteamIdentities)
                    {
                        matchTeam1SelectedSteamIdentity.matchId = realmatch.Id;
                    }
                    if(realmatch.MatchTeam0SelectedSteamIdentities.Any())
                        await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam0SelectedSteamIdentities,match.Id,0);
                    if(realmatch.MatchTeam1SelectedSteamIdentities.Any())
                        await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam1SelectedSteamIdentities,match.Id,1);
                }

                return true;
            }

            return false;
        }

        public async Task<ConnectionResult> StartMatchWithAuth(RconService.AuthType authType,
            PavlovServer server,
            Match match)
        {
            var connectionInfo = RconStatic.ConnectionInfoInternal(server.SshServer, authType, out var result);
            using var clientSsh = new SshClient(connectionInfo);
            using var clientSftp = new SftpClient(connectionInfo);
            try
            {
                var listOfSteamIdentietiesWhichCanPlay = match.MatchTeam0SelectedSteamIdentities;
                listOfSteamIdentietiesWhichCanPlay.AddRange(match.MatchTeam1SelectedSteamIdentities);
                var list = new List<string>();
                if (listOfSteamIdentietiesWhichCanPlay.Count <= 0 && match.MatchSelectedSteamIdentities.Count <= 0)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("There are no team members so no match will start!",
                        LogEventLevel.Fatal, _notifyService, result);
                    return result;
                }
                
                
                if (match.MatchSelectedSteamIdentities.Count > 0)
                    list = match.MatchSelectedSteamIdentities
                        .Select(x => Strings.Trim(x.SteamIdentityId)).ToList();
                else if (listOfSteamIdentietiesWhichCanPlay.Count > 0)
                    list = listOfSteamIdentietiesWhichCanPlay.Select(x => Strings.Trim(x.SteamIdentityId)).ToList();

                list = list.Distinct().ToList();
                //GetAllAdminsForTheMatch
                var mods = new List<string>();
                //Todo what if the match is not team based? there are no mods or admins?
                mods = listOfSteamIdentietiesWhichCanPlay.Where(x => x.OverWriteRole == "Mod" || x.OverWriteRole == "Admin").Select(x=>x.SteamIdentityId).ToList();

                
                //Write whitelist and set server settings

                try
                {
                    RconStatic.WriteFile(server.SshServer,
                        server.ServerFolderPath + FilePaths.WhiteList,
                        list.ToArray(), _notifyService);
                }
                catch (Exception e)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not write whitelist for the match! " +
                                                                    e.Message, LogEventLevel.Fatal, _notifyService,
                        result);
                }                
                
                try
                {
                    RconStatic.WriteFile(server.SshServer,
                        server.ServerFolderPath + FilePaths.ModList,
                        mods.ToArray(), _notifyService);
                }
                catch (Exception e)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not write modlist for the match! " +
                                                                    e.Message, LogEventLevel.Fatal, _notifyService,
                        result);
                }
                RconStatic.WriteFile(server.SshServer,
                    server.ServerFolderPath + FilePaths.BanList,
                    Array.Empty<string>(), _notifyService);


                var oldSettings = new PavlovServerGameIni();
                oldSettings.ReadFromFile(server, _notifyService);
                
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
                    Password = "",
                    BalanceTableURL = "",
                    bVerboseLogging = true,
                    bCompetitive = true,
                    MapRotation = new List<PavlovServerGameIniMap>
                    {
                        new()
                        {
                            MapLabel = match.MapId,
                            GameMode = match.GameMode
                        }
                    },
                    ApiKey = oldSettings.ApiKey
                };
                var map = await _mapsService.FindOne(match.MapId.Replace("UGC", ""));
                serverSettings.SaveToFile(server, new[]
                {
                    new ServerSelectedMap()
                    {
                        Map = map,
                        GameMode = match.GameMode
                    }
                }, _notifyService);
                await RconStatic.SystemDStart(server, _pavlovServerService);

                //StartWatchServiceForThisMatch
                match.Status = Status.StartetWaitingForPlayer;
                await Upsert(match);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start backgroundjob", LogEventLevel.Verbose,
                    _notifyService);
                
                BackgroundJob.Enqueue(
                    () => MatchInspector(match.Id)); // ChecjServerState
            }
            catch (Exception e)
            {
                RconStatic.ExcpetionHandlingSshSftp(server, _notifyService, e, result, clientSsh, clientSftp);
            }
            finally
            {
                clientSsh.Disconnect();
                clientSftp.Disconnect();
            }

            return result;
        }
        
        public async Task MatchInspector(int matchId)
        {
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("MatchInspector started!", LogEventLevel.Verbose,
                _notifyService);
            var match = await FindOne(matchId);

            try
            {
                var forceStopMaybe = "";
                try
                {
                    forceStopMaybe = await _rconService.SShTunnelGetAllInfoFromPavlovServer(match.PavlovServer,true);
                }
                catch (Exception)
                {
                }
                if (match.ForceSop||forceStopMaybe=="ForceStopNowUrgent") // ForceStopNowUrgent very bad practice
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("Endmatch!", LogEventLevel.Verbose, _notifyService);
                    await EndMatch(match.PavlovServer, match);
                    return;
                }

          
                switch (match.Status)
                {
                    case Status.StartetWaitingForPlayer:

                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("TryToStartMatch started!",
                            LogEventLevel.Verbose, _notifyService);
                        await TryToStartMatch(match.PavlovServer, match);
                        break;
                    case Status.OnGoing:

                        DataBaseLogger.LogToDatabaseAndResultPlusNotify("OnGoing!", LogEventLevel.Verbose,
                            _notifyService);
                        var serverInfo = await _pavlovServerInfoService.FindServer(match.PavlovServer.Id);
                        match.PlayerResults =
                            (await _pavlovServerPlayerService.FindAllFromServer(match.PavlovServer.Id)).ToList();
                        match.EndInfo = serverInfo;
                        await Upsert(match);

                        if (serverInfo.Team0Score == "10"||serverInfo.Team1Score == "10")
                        {
                            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Round ended!", LogEventLevel.Verbose,
                                _notifyService);
                            match.PlayerResults =
                                (await _pavlovServerPlayerService.FindAllFromServer(match.PavlovServer.Id)).ToList();
                            match.EndInfo = serverInfo;
                            await EndMatch(match.PavlovServer, match);
                            return;
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
            }

            if (match.Status != Status.Finshed)
                BackgroundJob.Schedule(
                    () => MatchInspector(match.Id),
                    new TimeSpan(0, 0, 1)); // ChecjServerState
        }

        
        public async Task<bool> SaveStatsFromLogs(int matchId)
        {
            var match = await FindOne(matchId);
            match.MatchTeam0SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 0))
                .ToList();
            match.MatchTeam1SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 1))
                .ToList();
            match.MatchSelectedSteamIdentities =
                (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(matchId))
                .ToList();
            var logs = await RconStatic.GetServerLog(match.PavlovServer, _pavlovServerService);
            //Todo if that works on other servers? maybe not?
            var realLogs = logs.answer;
            realLogs = realLogs.Substring(0,realLogs.LastIndexOf("StatManagerLog: End Stat Dump", StringComparison.Ordinal))+"StatManagerLog: End Stat Dump";
            int idx = realLogs.LastIndexOf("StatManagerLog: {", StringComparison.Ordinal);

            if (idx != -1)
            {
                var idx2 = realLogs.LastIndexOf("StatManagerLog: End Stat Dump", StringComparison.Ordinal);
                int length = idx2 - idx;
                var tmp = realLogs.Substring(idx, length);
                var fisrt = tmp.IndexOf("{", StringComparison.Ordinal);
                var last = tmp.LastIndexOf("}", StringComparison.Ordinal);
                int length2 = last - fisrt;
                var jsonString = tmp.Substring((fisrt), length2+1);
                var stats = JsonConvert.DeserializeObject<EndStatsFromLogs>(
                    jsonString.Trim());
                var finsihedPlayerList = new List<PavlovServerPlayer>();
                if (stats != null)
                    foreach (var playerModelEndStatsFromLogs in stats.allStats)
                    {
                        var tmpMatchTeamSelectedSteamIdentity = match.MatchTeam0SelectedSteamIdentities?.FirstOrDefault(x =>
                            x.SteamIdentityId == playerModelEndStatsFromLogs.uniqueId);
                        if(tmpMatchTeamSelectedSteamIdentity==null)
                            tmpMatchTeamSelectedSteamIdentity = match.MatchTeam1SelectedSteamIdentities?.FirstOrDefault(x =>
                            x.SteamIdentityId == playerModelEndStatsFromLogs.uniqueId);

                        var teamId = tmpMatchTeamSelectedSteamIdentity?.TeamId;
                        var realTeamId = 0;
                        if (teamId != null) realTeamId = (int)teamId;
                        var oldLog = match.PlayerResults.FirstOrDefault(x => x.UniqueId == playerModelEndStatsFromLogs.uniqueId);
                        finsihedPlayerList.Add(new PavlovServerPlayer()
                        {
                            Username = (await _steamIdentityService.FindOne(playerModelEndStatsFromLogs.uniqueId)).Name,
                            UniqueId = oldLog!=null ? oldLog.UniqueId : "",
                            Kills = playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Kill") !=null? (int)playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Kill")?.amount: 0,
                            Deaths = playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Death") !=null? (int)playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Death")?.amount : 0,
                            Headshot = playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Headshot") !=null? (int)playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Headshot")?.amount : 0,
                            Score = playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Experience")!=null ? (int)playerModelEndStatsFromLogs.stats.FirstOrDefault(x=>x.statType=="Experience")?.amount : 0,
                            TeamId = realTeamId,
                            Cash = oldLog!=null ? oldLog.Cash : "0"
                        });
                    }
                match.PlayerResults = finsihedPlayerList;
                await Upsert(match);
                return true;
            }
            return false;
        }
        private async Task EndMatch(PavlovServer server, Match match)
        {
            try
            {
                await SaveStatsFromLogs(match.Id);
            }
            catch (Exception)
            {
                //can be done later manualy but should not be breaking otherwise the server runs more and we may lose tha stats in the logs
            }

            match.Status = Status.Finshed;
            await Upsert(match);
            await RconStatic.SystemDStop(server, _pavlovServerService);
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Stopped server!", LogEventLevel.Verbose, _notifyService);
        }


        private async Task TryToStartMatch(PavlovServer server, Match match)
        {
            var playerList = (await _pavlovServerPlayerService.FindAllFromServer(server.Id)).ToList();
            DataBaseLogger.LogToDatabaseAndResultPlusNotify(" playerlistcount = " + playerList.Count(),
                LogEventLevel.Verbose, _notifyService);
            DataBaseLogger.LogToDatabaseAndResultPlusNotify(" match.PlayerSlots  = " + match.PlayerSlots,
                LogEventLevel.Verbose, _notifyService);
            if (playerList.Count() == match.PlayerSlots || match.ForceStart) //All Player are here
            {
                //Do Players in the right team
                if (match.GameMode == "SND")
                {
                    ForceTeamsToTheRightPlace(server, match, playerList);
                }
                //All Players are on the right team now
                //ResetSND
                    
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("start ResetSND!", LogEventLevel.Verbose,
                    _notifyService);
                //Todo for every thing else than SND

                if (match.GameMode == "SND")
                {
                    Thread.Sleep(5000); // so game has time do the team switch
                    SendCommandTillDone(server, "ResetSND");
                }
                else
                {
                    SendCommandTillDone(server, "RotateMap");
                    var gotAnswer = GameModes.HasTeams.TryGetValue(match.GameMode, out var hasTeams);
                    if (gotAnswer && hasTeams)
                    {
                        //The players shoud just not win the round befor it forced all the team changes
                        Thread.Sleep(30000);
                        ForceTeamsToTheRightPlace(server, match, playerList,true);
                        //After forcing all stats are back byside the teamscore stats so until they have not done team score already it should be fine 
                    }
                }
                //Todo: Give start sign waiting for: https://pavlovvr.featureupvote.com/suggestions/229367/motd-and-the-possibility-to-give-the-player-a-message-over-rcon
                match.Status = Status.OnGoing;
                await Upsert(match);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="match"></param>
        /// <param name="playerList"></param>
        /// <param name="twoTimes">If its not SND we should team changes 2 times cause than we reset the stats</param>
        private void ForceTeamsToTheRightPlace(PavlovServer server, Match match, List<PavlovServerPlayer> playerList,bool twoTimes = false)
        {
            foreach (var pavlovServerPlayer in playerList)
            {
                var team0 = match.MatchTeam0SelectedSteamIdentities.FirstOrDefault(x =>
                    x.SteamIdentityId == pavlovServerPlayer.UniqueId);
                var team1 = match.MatchTeam1SelectedSteamIdentities.FirstOrDefault(x =>
                    x.SteamIdentityId == pavlovServerPlayer.UniqueId);
                if (team0 != null)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("SwitchTeam 0 " + pavlovServerPlayer.UniqueId,
                        LogEventLevel.Verbose, _notifyService);
                    if (twoTimes)
                    {
                        SendCommandTillDone(server, "SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                        Thread.Sleep(1000);
                    }
                    SendCommandTillDone(server, "SwitchTeam 0 " + pavlovServerPlayer.UniqueId);
                }
                else if (team1 != null)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("SwitchTeam 1 " + pavlovServerPlayer.UniqueId,
                        LogEventLevel.Verbose, _notifyService);                    
                    if (twoTimes)
                    {
                        SendCommandTillDone(server, "SwitchTeam 0 " + pavlovServerPlayer.UniqueId);
                        Thread.Sleep(1000);
                    }
                    SendCommandTillDone(server, "SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                }
            }
        }

        private string SendCommandTillDone(PavlovServer server,
            string command, int timeoutInSeconds = 60)
        {
            var task = Task.Run(() => SendCommandTillDoneChild(server, command));
            if (task.Wait(TimeSpan.FromSeconds(timeoutInSeconds)))
                return task.Result;
            throw new Exception("Timed out");
        }

        private async Task<string> SendCommandTillDoneChild(PavlovServer server,
            string command)
        {
            while (true)
                try
                {
                    var result = await RconStatic.SendCommandSShTunnel(server, command, _notifyService);
                    return result;
                }
                catch (CommandException e)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
                    throw;
                }
        }

        public async Task StartMatch(int matchId)
        {
            var exceptions = new List<Exception>();
            try
            {
                var match = await FindOne(matchId);
                match.MatchTeam0SelectedSteamIdentities =
                    (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 0))
                    .ToList();
                match.MatchTeam1SelectedSteamIdentities =
                    (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 1))
                    .ToList();
                match.MatchSelectedSteamIdentities =
                    (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(matchId))
                    .ToList();
                var server = await _pavlovServerService.FindOne(match.PavlovServer.Id);

                var connectionResult = new ConnectionResult();
                if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                    (server.SshServer.SshKeyFileName==null||!server.SshServer.SshKeyFileName.Any()) &&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.PrivateKeyPassphrase, server, match);

                if (!connectionResult.Success && (server.SshServer.SshKeyFileName==null||!server.SshServer.SshKeyFileName.Any())&&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.PrivateKey, server, match);

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                    !string.IsNullOrEmpty(server.SshServer.SshPassword))
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.UserPass, server, match);

                if (!connectionResult.Success)
                    if (connectionResult.errors.Count <= 0)
                        throw new CommandException("Could not connect to server!");
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }
        
        

        public async Task<Match[]> FindAllMatchesWhereTheUserHasRights(ClaimsPrincipal cp, LiteDbUser user)
        {
            //var matches = matchSelectedSteamIdentities.Select(x=>x.matchId).Distinct()
            var matches = await FindAll();
            if (cp.IsInRole("Admin") || cp.IsInRole("Mod") || cp.IsInRole("Captain"))
            {
                return matches;
            }
            var ownSteamIdentity = await _steamIdentityService.FindOne(user.Id);

            var tmpMatches = new List<Match>();
            foreach (var match in matches)
            {
                var matchTeamSelectedSteamIdentities = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,0)).ToList();
                matchTeamSelectedSteamIdentities.AddRange( await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,1));

                var role = matchTeamSelectedSteamIdentities.Where(x => x.SteamIdentityId == ownSteamIdentity.Id)
                    .Select(x => x.OverWriteRole).FirstOrDefault();


                if (!string.IsNullOrEmpty(role)&&(role=="Mod"||role=="Admin"))
                {
                    tmpMatches.Add(match);
                }
            }

            return tmpMatches.ToArray();

        }
        public async Task RestartAllTheInspectorsForTheMatchesThatAreOnGoing()
        {
            var matches = (await FindAll()).Where(x =>
                x.Status == Status.OnGoing || x.Status == Status.StartetWaitingForPlayer);

            foreach (var match in matches)
            {
                BackgroundJob.Schedule(
                    () => MatchInspector(match.Id),
                    new TimeSpan(0, 0, 5)); 
            }
        }

        public async Task<Match[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<Match>("Match")
                .Include(x => x.PavlovServer)
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

        public async Task<Match> FindOne(int id)
        {
            var selected = await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(id);
            var match = await _liteDb.LiteDatabaseAsync.GetCollection<Match>("Match")
                .Include(x => x.Team0)
                .Include(x => x.Team1)
                .FindOneAsync(x => x.Id == id);
            if (match == null) return null;
            match.PavlovServer = await _pavlovServerService.FindOne(match.PavlovServer.Id);
            match.MatchSelectedSteamIdentities = selected.ToList();
            return match;
        }

        public async Task<MatchViewModel> PrepareViewModel(Match oldMatch)
        {
            var match = new MatchViewModel
            {
                Id = oldMatch.Id,
                Name = oldMatch.Name,
                MapId = oldMatch.MapId,
                ForceSop = oldMatch.ForceSop,
                ForceStart = oldMatch.ForceStart,
                TimeLimit = oldMatch.TimeLimit,
                PlayerSlots = oldMatch.PlayerSlots,
                GameMode = oldMatch.GameMode,
                Team0 = oldMatch.Team0,
                Team1 = oldMatch.Team1,
                PavlovServer = oldMatch.PavlovServer,
                Status = oldMatch.Status,
                Team0Id = oldMatch.Team0?.Id,
                Team1Id = oldMatch.Team1?.Id
            };
            if (oldMatch.PavlovServer != null)
                match.PavlovServerId = oldMatch.PavlovServer.Id;
            match.AllTeams = (await _teamService.FindAll()).ToList();
            match.AllPavlovServers = (await _pavlovServerService.FindAll()).Where(x => x.ServerType == ServerType.Event)
                .ToList(); // and where no match is already running

            match.MatchSelectedSteamIdentities =
                (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(oldMatch.Id)).ToList();
            match.MatchTeam0SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(oldMatch.Id, 0))
                .ToList();
            match.MatchTeam1SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(oldMatch.Id, 1))
                .ToList();
            return match;
        }

        public async Task SaveMatchResult(PavlovServerPlayerListPublicViewModel match, Match realMatch)
        {
            if (match.ServerInfo != null)
            {
                realMatch.EndInfo.Team0Score = match.ServerInfo.Team0Score;
                realMatch.EndInfo.Team1Score = match.ServerInfo.Team1Score;
            }

            if (match.PlayerList != null)
                foreach (var playerModelExtended in match.PlayerList)
                {
                    var player =
                        realMatch.PlayerResults.FirstOrDefault(x => x.UniqueId == playerModelExtended.UniqueId);
                    if (player != null)
                    {
                        player.Kills = playerModelExtended.Kills;
                        player.Deaths = playerModelExtended.Deaths;
                        player.Assists = playerModelExtended.Assists;
                    }
                }

            await Upsert(realMatch);
        }

        public async Task<bool> Upsert(Match match)
        {
            var result = false;
            if (match.Id == 0)
            {
                var tmp = await _liteDb.LiteDatabaseAsync.GetCollection<Match>("Match")
                    .InsertAsync(match);
                if (tmp > 0) result = true;
            }
            else
            {
                result = await _liteDb.LiteDatabaseAsync.GetCollection<Match>("Match")
                    .UpdateAsync(match);
            }

            return result;
        }

        public async Task<bool> Delete(int id)
        {
            await _matchSelectedTeamSteamIdentitiesService.RemoveFromMatch(id);
            await _matchSelectedSteamIdentitiesService.RemoveFromMatch(id);
            return await _liteDb.LiteDatabaseAsync.GetCollection<Match>("Match").DeleteAsync(id);
        }

        public async Task<bool> CanBeDeleted(int id)
        {
            var match = await FindOne(id);
            if (match == null) return false;
            return match.Status != Status.OnGoing;
        }
    }
}