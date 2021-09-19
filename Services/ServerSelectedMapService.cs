using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedMapService
    {
        private readonly ILiteDbIdentityContext _liteDb;

        public ServerSelectedMapService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }


        public async Task<IEnumerable<ServerSelectedMap>> FindAllFrom(PavlovServer pavlovServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == pavlovServer.Id);
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
            return await  _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .UpdateAsync(serverSelectedMap);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap").DeleteAsync(id);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .DeleteManyAsync(x => x.PavlovServer == server);
        }
    }
}