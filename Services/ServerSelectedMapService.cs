using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
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

        public async Task<IEnumerable<ServerSelectedMap>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .FindAll();
        }

        public async Task<IEnumerable<ServerSelectedMap>> FindAllFrom(PavlovServer sshServer)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .Find(x => x.PavlovServer.Id == sshServer.Id);
        }

        public async Task<IEnumerable<ServerSelectedMap>> FindAllFrom(Map map)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .Find(x => x.Map.Id == map.Id);
        }

        public async Task<ServerSelectedMap> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<ServerSelectedMap> FindSelectedMap(int serverId, string mapId)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap").Include(x => x.Map)
                .Include(x => x.PavlovServer)
                .Find(x => x.Map.Id == mapId && x.PavlovServer.Id == serverId).FirstOrDefault();
        }

        public async Task<int> Insert(ServerSelectedMap serverSelectedMap)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Insert(serverSelectedMap);
        }

        public async Task<int> Upsert(List<ServerSelectedMap> serverSelectedMaps)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Upsert(serverSelectedMaps);
        }

        public async Task<bool> Update(ServerSelectedMap serverSelectedMap)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Update(serverSelectedMap);
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap").Delete(id);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .DeleteMany(x => x.PavlovServer == server);
        }
    }
}