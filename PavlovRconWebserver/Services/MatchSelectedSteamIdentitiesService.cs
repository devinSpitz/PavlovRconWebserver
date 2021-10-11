using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedSteamIdentitiesService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public MatchSelectedSteamIdentitiesService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<int> RemoveFromMatch(int matchId)
        {
            return await _liteDb.LiteDatabaseAsync
                .GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .DeleteManyAsync(x => x.matchId == matchId);
        }

        public async Task<MatchSelectedSteamIdentity[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAllAsync()).ToArray();
        }

        public async Task<MatchSelectedSteamIdentity[]> FindAllSelectedForMatch(int matchId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .FindAllAsync()).Where(x => x.matchId == matchId).ToArray();
        }

        public async Task<int> Upsert(List<MatchSelectedSteamIdentity> matchSelectedSteamIdentities, int matchId)
        {
            if (matchId != 0)
                await _liteDb.LiteDatabaseAsync.GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                    .DeleteManyAsync(x => x.matchId == matchId);

            return await _liteDb.LiteDatabaseAsync
                .GetCollection<MatchSelectedSteamIdentity>("MatchSelectedSteamIdentity")
                .UpsertAsync(matchSelectedSteamIdentities);
        }
    }
}