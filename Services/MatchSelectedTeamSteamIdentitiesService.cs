using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedTeamSteamIdentitiesService
    {
        
        private ILiteDbIdentityContext _liteDb;

        public MatchSelectedTeamSteamIdentitiesService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<MatchTeamSelectedSteamIdentity>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .FindAll();
        }
        
        public async Task<IEnumerable<MatchTeamSelectedSteamIdentity>> FindAllSelectedForMatchAndTeam(int matchId,int teamId)
        {
            return _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .Include(x=>x.Match)
                .FindAll().Where(x=>x.Match.Id==matchId&&x.TeamId == teamId);
        }
        
        public async Task<int> Upsert(List<MatchTeamSelectedSteamIdentity> matchTeamSelectedSteamIdentity)
        {
            return _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .Include(x => x.Match)
                .Upsert(matchTeamSelectedSteamIdentity);
            
        }

    }
}