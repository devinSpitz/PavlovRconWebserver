using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchService
    {
        private readonly ILiteDbIdentityContext _liteDb;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;

        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly TeamService _teamService;


        public MatchService(ILiteDbIdentityContext liteDbContext,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentitiesService,
            PavlovServerService pavlovServerService,
            SteamIdentityService steamIdentityService,
            TeamService teamService,
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


        public async Task StartMatch(int matchId, string connectionString)
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
                    connectionResult = await RconStatic.StartMatchWithAuth(
                        RconService.AuthType.PrivateKeyPassphrase, server, match, connectionString);

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                    connectionResult = await RconStatic.StartMatchWithAuth(
                        RconService.AuthType.PrivateKey, server, match, connectionString);

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                    !string.IsNullOrEmpty(server.SshServer.SshPassword))
                    connectionResult = await RconStatic.StartMatchWithAuth(
                        RconService.AuthType.UserPass, server, match, connectionString);

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
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Include(x => x.Team0)
                .Include(x => x.Team1)
                .Find(x => x.Id == id).FirstOrDefault();
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