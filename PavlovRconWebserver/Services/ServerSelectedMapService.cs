using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedMapService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public ServerSelectedMapService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }


        public async Task<ServerSelectedMap[]> FindAllFrom(PavlovServer pavlovServer)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.Map != null && x.PavlovServer.Id == pavlovServer.Id)).ToArray();
        }

        public async Task<int> Insert(ServerSelectedMap serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .InsertAsync(serverSelectedMap);
        }

        public async Task<int> Upsert(List<ServerSelectedMap> serverSelectedMaps)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .UpsertAsync(serverSelectedMaps);
        }

        public async Task<bool> Update(ServerSelectedMap serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .UpdateAsync(serverSelectedMap);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .DeleteAsync(id);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .DeleteManyAsync(x => x.PavlovServer.Id == server.Id);
        }
    }
}