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
        private ILiteDbIdentityContext _liteDb;
        
        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly RconService _rconService;
        private readonly MapsService _mapsService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        
        public MatchService(ILiteDbIdentityContext liteDbContext,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService,
            PavlovServerService pavlovServerService,
            RconService rconService,
            MapsService mapsService,
            PavlovServerPlayerService pavlovServerPlayerService,
            PavlovServerInfoService pavlovServerInfoService
        )
        {
            _liteDb = liteDbContext;
            _matchSelectedTeamSteamIdentitiesService = matchSelectedTeamSteamIdentitiesService;
            _pavlovServerService = pavlovServerService;
            _rconService = rconService;
            _mapsService = mapsService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerInfoService = pavlovServerInfoService;

        }

        
        
        public async Task StartMatch(int matchId,string connectionString)
        {
            var exceptions = new List<Exception>();
            try
            {
                var match = await FindOne(matchId);
                //Todo: get selected steamidentitys
                match.MatchTeam0SelectedSteamIdentities = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 0)).ToList();
                match.MatchTeam1SelectedSteamIdentities = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(matchId, 1)).ToList();
                var server = await _pavlovServerService.FindOne(match.PavlovServer.Id);
                
                var connectionResult = new ConnectionResult();
                 if (!string.IsNullOrEmpty(server.SshServer.SshPassphrase) &&
                !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                !string.IsNullOrEmpty(server.SshServer.SshUsername))
                 {

                     connectionResult = await RconStatic.StartMatchWithAuth(
                         RconService.AuthType.PrivateKeyPassphrase,server,match,connectionString);
                }

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshKeyFileName) &&
                    File.Exists("KeyFiles/" + server.SshServer.SshKeyFileName) &&
                    !string.IsNullOrEmpty(server.SshServer.SshUsername))
                {
                    connectionResult = await RconStatic.StartMatchWithAuth(
                        RconService.AuthType.PrivateKey,server,match,connectionString);
                }

                if (!connectionResult.Success && !string.IsNullOrEmpty(server.SshServer.SshUsername) &&
                    !string.IsNullOrEmpty(server.SshServer.SshPassword))
                {
                    connectionResult = await RconStatic.StartMatchWithAuth(
                        RconService.AuthType.UserPass,server,match,connectionString);
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
        
        
        public async Task<IEnumerable<Match>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Include(x=>x.PavlovServer)
                .FindAll().OrderByDescending(x=>x.Id);
        }

        public async Task<Match> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Include(x=>x.Team0)
                .Include(x=>x.Team1)
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<bool> Upsert(Match match)
        {
            var result = false;
            if (match.Id == 0 || match.Id == null)
            {
                var tmp = _liteDb.LiteDatabase.GetCollection<Match>("Match")
                    .Insert(match);
                if (tmp > 0)
                {
                    result = true;
                }
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