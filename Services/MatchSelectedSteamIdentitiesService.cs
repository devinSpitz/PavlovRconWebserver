using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedSteamIdentitiesService
    {
        private readonly ILiteDbIdentityContext _liteDb;

        public MatchSelectedSteamIdentitiesService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<int> RemoveFromMatch(int matchId)
        {
            var tmp = (await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAllAsync()).ToList();


            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .DeleteManyAsync(x => x.matchId == matchId));
        }

        public async Task<IEnumerable<MatchSelectedSteamIdentity>> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAllAsync());
        }

        public async Task<IEnumerable<MatchSelectedSteamIdentity>> FindAllSelectedForMatch(int matchId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAllAsync()).Where(x => x.matchId == matchId);
        }

        public async Task<int> Upsert(List<MatchSelectedSteamIdentity> matchSelectedSteamIdentities, int matchId)
        {
            if (matchId != 0)
                await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                    .DeleteManyAsync(x => x.matchId == matchId);

            return await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .UpsertAsync(matchSelectedSteamIdentities);
        }
    }
}