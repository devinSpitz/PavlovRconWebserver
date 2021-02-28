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
                .Include(x=>x.Match)
                .FindAll().Where(x=>x.Match.Id==matchId);
        }
        
        public async Task<int> Upsert(List<MatchSelectedSteamIdentity> matchSelectedSteamIdentities)
        {
            return _liteDb.LiteDatabase.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .Include(x => x.Match)
                .Upsert(matchSelectedSteamIdentities);
            
        }

    }
}