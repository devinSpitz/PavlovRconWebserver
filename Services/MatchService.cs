using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hangfire;
using LiteDB.Identity.Database;
using Microsoft.VisualBasic;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PavlovRconWebserver.Services
{
    public class MatchService
    {
        private readonly ILiteDbIdentityContext _liteDb;
        private readonly MapsService _mapsService;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;

        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly RconService _rconService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly TeamService _teamService;


        public MatchService(ILiteDbIdentityContext liteDbContext,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentitiesService,
            PavlovServerService pavlovServerService,
            SteamIdentityService steamIdentityService,
            RconService rconService,
            MapsService mapsService,
            TeamService teamService,
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService
        )
        {
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
                                TeamId = 0
                            });
                        else
                            return true;
                    }

                    foreach (var team1SelectedSteamIdentity in match.MatchTeam1SelectedSteamIdentitiesStrings)
                    {
                        var tmp = realmatch.Team1.TeamSelectedSteamIdentities.FirstOrDefault(x =>
                            x.SteamIdentity.Id.ToString() == team1SelectedSteamIdentity);
                        if (tmp != null)
                            realmatch.MatchTeam0SelectedSteamIdentities.Add(new MatchTeamSelectedSteamIdentity
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = team1SelectedSteamIdentity,
                                TeamId = 1
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


            //

            var bla = await Upsert(realmatch);

            if (bla)
            {
                // First remove Old TeamSelected and Match selected stuff

                if (realmatch.MatchSelectedSteamIdentities.Count > 0)
                {
                    foreach (var realmatchMatchSelectedSteamIdentity in realmatch.MatchSelectedSteamIdentities)
                        realmatchMatchSelectedSteamIdentity.matchId = realmatch.Id;

                    await _matchSelectedSteamIdentitiesService.Upsert(realmatch.MatchSelectedSteamIdentities,
                        realmatch.Id);
                }

                // Then write the new ones

                if (realmatch.MatchTeam0SelectedSteamIdentities.Count > 0)
                {
                    foreach (var matchTeam0SelectedSteamIdentities in realmatch.MatchTeam0SelectedSteamIdentities)
                        matchTeam0SelectedSteamIdentities.matchId = realmatch.Id;

                    await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam0SelectedSteamIdentities,
                        realmatch.Id, (int) match.Team0Id);
                }

                if (realmatch.MatchTeam1SelectedSteamIdentities.Count > 0)
                {
                    foreach (var matchTeam1SelectedSteamIdentities in realmatch.MatchTeam1SelectedSteamIdentities)
                        matchTeam1SelectedSteamIdentities.matchId = realmatch.Id;

                    await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam1SelectedSteamIdentities,
                        realmatch.Id, (int) match.Team1Id);
                }

                return true;
            }

            return false;
        }

        public async Task<ConnectionResult> StartMatchWithAuth(RconService.AuthType authType,
            PavlovServer server,
            Match match)
        {
            var connectionInfo = RconStatic.ConnectionInfoInternal(server, authType, out var result);
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
                        list = match.MatchSelectedSteamIdentities
                            .Select(x => Strings.Trim(x.SteamIdentityId + ";" + Environment.NewLine)).ToList();
                    else if (listOfSteamIdentietiesWhichCanPlay.Count > 0)
                        list = listOfSteamIdentietiesWhichCanPlay.Select(x => Strings.Trim(x.SteamIdentityId)).ToList();
                }

                //Write whitelist and set server settings

                var whitelist = "";
                try
                {
                    RconStatic.WriteFile(server,
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
                var map = await _mapsService.FindOne(match.MapId.Replace("UGC", ""));
                serverSettings.SaveToFile(server, new List<ServerSelectedMap>
                {
                    new ServerSelectedMap
                    {
                        Map = map,
                        GameMode = match.GameMode
                    }
                });
                await RconStatic.SystemDStart(server,_pavlovServerService);

                //StartWatchServiceForThisMatch
                match.Status = Status.StartetWaitingForPlayer;
                await Upsert(match);
                Console.WriteLine("Start backgroundjob");
                BackgroundJob.Schedule(
                    () => MatchInspector(authType, match.Id),
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


        public async Task MatchInspector(RconService.AuthType authType,
            int matchId)
        {
            Console.WriteLine("MatchInspector started!");
            var match = await FindOne(matchId);

            try
            {
                if (match.ForceSop)
                {
                    Console.WriteLine("Endmatch!");
                    await EndMatch(match.PavlovServer, match);
                    return;
                }

                await _rconService.SShTunnelGetAllInfoFromPavlovServer(match.PavlovServer);
                switch (match.Status)
                {
                    case Status.StartetWaitingForPlayer:

                        Console.WriteLine("TryToStartMatch started!");
                        await TryToStartMatch(match.PavlovServer, match);
                        break;
                    case Status.OnGoing:

                        Console.WriteLine("OnGoing!");
                        var serverInfo = await _pavlovServerInfoService.FindServer(match.PavlovServer.Id);
                        match.PlayerResults =
                            (await _pavlovServerPlayerService.FindAllFromServer(match.PavlovServer.Id)).ToList();
                        match.EndInfo = serverInfo;
                        await Upsert(match);

                        if (serverInfo.RoundState == "Ended")
                        {
                            Console.WriteLine("Round ended!");
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
                Console.WriteLine(e);
            }

            if (match.Status != Status.Finshed)
                BackgroundJob.Schedule(
                    () => MatchInspector(authType, match.Id),
                    new TimeSpan(0, 0, 5)); // ChecjServerState
        }

        private async Task EndMatch(PavlovServer server, Match match)
        {
            match.Status = Status.Finshed;
            await Upsert(match);
            await RconStatic.SystemDStop(server,_pavlovServerService);
            Console.WriteLine("Stopped server!");
        }


        private async Task TryToStartMatch(PavlovServer server, Match match)
        {
            var playerList = (await _pavlovServerPlayerService.FindAllFromServer(server.Id)).ToList();
            Console.WriteLine(" playerlistcount = " + playerList.Count());
            Console.WriteLine(" match.PlayerSlots  = " + match.PlayerSlots);
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
                        await SendCommandTillDone(server, "SwitchTeam 0 " + pavlovServerPlayer.UniqueId);
                    }
                    else if (team1 != null)
                    {
                        Console.WriteLine("SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                        await SendCommandTillDone(server, "SwitchTeam 1 " + pavlovServerPlayer.UniqueId);
                    }
                }
                //All Players are on the right team now
                //ResetSND

                Console.WriteLine("start ResetSND!");
                await SendCommandTillDone(server, "ResetSND");
                match.Status = Status.OnGoing;
                await Upsert(match);
            }
        }

        private async Task<string> SendCommandTillDone(PavlovServer server,
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
                    var result = await RconStatic.SendCommandSShTunnel(server, command);
                    return result;
                }
                catch (CommandException e)
                {
                    Console.WriteLine(e);
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
                var server = await _pavlovServerService.FindOne(match.PavlovServer.Id);

                var connectionResult = new ConnectionResult();
                if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                    !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                    connectionResult = await StartMatchWithAuth(
                        RconService.AuthType.PrivateKeyPassphrase, server, match);

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
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


        public async Task<IEnumerable<Match>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Include(x => x.PavlovServer)
                .FindAll().OrderByDescending(x => x.Id);
        }

        public async Task<Match> FindOne(int id)
        {
            var selected = await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(id);
            var match = _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Include(x => x.Team0)
                .Include(x => x.Team1)
                .Find(x => x.Id == id).FirstOrDefault();
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
                var tmp = _liteDb.LiteDatabase.GetCollection<Match>("Match")
                    .Insert(match);
                if (tmp > 0) result = true;
            }
            else
            {
                result = _liteDb.LiteDatabase.GetCollection<Match>("Match")
                    .Update(match);
            }

            return result;
        }

        public async Task<bool> Delete(int id)
        {
            await _matchSelectedTeamSteamIdentitiesService.RemoveFromMatch(id);
            await _matchSelectedSteamIdentitiesService.RemoveFromMatch(id);
            return _liteDb.LiteDatabase.GetCollection<Match>("Match").Delete(id);
        }

        public async Task<bool> CanBedeleted(int id)
        {
            var match = await FindOne(id);
            if (match == null) return false;
            if (match.Status == Status.OnGoing) return false;
            return true;
        }
    }
}