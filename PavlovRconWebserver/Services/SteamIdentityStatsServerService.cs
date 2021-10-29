
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class SteamIdentityStatsServerService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public SteamIdentityStatsServerService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }
        public async Task<SteamIdentityStatsServer[]> FindAllFromServer(int id)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer")
                .FindAsync(x=>x.serverId==id)).ToArray();
        }
        public async Task<int> Insert(SteamIdentityStatsServer steamIdentityStatsServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer")
                .InsertAsync(steamIdentityStatsServer);
        }
        public async Task<bool> Update(SteamIdentityStatsServer steamIdentityStatsServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer")
                .UpdateAsync(steamIdentityStatsServer);
        }
        public async Task<bool> Delete(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer").DeleteAsync( new ObjectId(id));
        }
    }
}