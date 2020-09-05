using System.Collections.Generic;
using System.Linq;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedMapService
    {
        
        private ILiteDbIdentityContext _liteDb;

        public ServerSelectedMapService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public IEnumerable<ServerSelectedMap> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .FindAll();
        }

        public IEnumerable<ServerSelectedMap> FindAllFrom(RconServer rconServer)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Include(x=>x.RconServer)
                .Find(x=>x.RconServerId == rconServer.Id);
        }
        
        public IEnumerable<ServerSelectedMap> FindAllFrom(Map map)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Find(x=>x.MapId == map.Id);
        }

        public ServerSelectedMap FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public ServerSelectedMap FindSelectedMap(int serverId,string mapId )
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Find(x => x.MapId == mapId && x.RconServerId == serverId).FirstOrDefault();
        }
        
        public int Insert(ServerSelectedMap serverSelectedMap)
        {

            
            //serverSelectedMap = validateRconServer(serverSelectedMap);
            
            
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Insert(serverSelectedMap);
        }
        public int Upsert(List<ServerSelectedMap> serverSelectedMaps)
        {

            
            //serverSelectedMap = validateRconServer(serverSelectedMap);
            
            
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Upsert(serverSelectedMaps);
        }

        public bool Update(ServerSelectedMap serverSelectedMap)
        {

            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap")
                .Update(serverSelectedMap); 
            
           
        }

        public bool Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedMap>("ServerSelectedMap").Delete(id);
        }
    }
}