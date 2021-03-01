using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedSteamIdentitiesService
    {
        
        private ILiteDbIdentityContext _liteDb;

        public MatchSelectedSteamIdentitiesService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<MatchSelectedSteamIdentity>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAll();
        }
        
        public async Task<IEnumerable<MatchSelectedSteamIdentity>> FindAllSelectedForMatch(int matchId)
        {
            return _liteDb.LiteDatabase.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAll().Where(x=>x.matchId==matchId);
        }
        
        public async Task<int> Upsert(List<MatchSelectedSteamIdentity> matchSelectedSteamIdentities,int matchId)
        {
            if(matchId!=0)
                _liteDb.LiteDatabase.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                    .DeleteMany(x=>x.matchId==matchId);
            
            return _liteDb.LiteDatabase.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .Upsert(matchSelectedSteamIdentities);
            
        }

    }
}