using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
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
                .FindAll().Where(x=>x.matchId==matchId&&x.TeamId == teamId);
        }
        
        public async Task<int> Upsert(List<MatchTeamSelectedSteamIdentity> matchTeamSelectedSteamIdentity, int matchId,int teamId)
        {
            if(matchId!=0)
                _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                    .DeleteMany(x=>x.matchId==matchId && x.TeamId==teamId);
            
            return _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .Include(x =>x.matchId==matchId && x.TeamId==teamId)
                .Upsert(matchTeamSelectedSteamIdentity);
        }
        
                
        public async Task<int> RemoveFromMatch(int matchId)
        {
            var tmp = _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity").FindAll().ToList();
            
            
            return _liteDb.LiteDatabase.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .DeleteMany(x=>x.matchId==matchId);
        }

    }
}