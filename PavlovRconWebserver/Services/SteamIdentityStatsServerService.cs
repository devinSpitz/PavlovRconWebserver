
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
        
        
        public async Task<SteamIdentityStatsServer[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer")
                .FindAllAsync()).ToArray();
        }
        public async Task<SteamIdentityStatsServer[]> FindAllFromServer(int id)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer")
                .FindAsync(x=>x.ServerId==id)).ToArray();
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
        public async Task<int> DeleteForServer(int serverId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer").DeleteManyAsync(x=>x.ServerId == serverId);
        }
        public async Task<int> DeleteForSteamId(string steamId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer").DeleteManyAsync(x=>x.SteamId == steamId);
        }        
        public async Task<SteamIdentityStatsServer[]> GetForSteamId(string steamId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentityStatsServer>("SteamIdentityStatsServer").FindAsync(x=>x.SteamId == steamId)).ToArray();
        }
    }
}