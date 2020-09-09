using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerService
    {
        
        private ILiteDbIdentityContext _liteDb;
        
        
        public PavlovServerService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<PavlovServer>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .FindAll().OrderByDescending(x=>x.Id);
        }
        
        public async Task<IEnumerable<PavlovServer>> FindAllFrom(int rconServerId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .FindAll().Where(x=>x.RconServerId == rconServerId);
        }
        public async Task<PavlovServer> FindOne(long id,RconServerSerivce rconServerSerivce)
        {
            var pavlovServer =  _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Find(x => x.Id == id).FirstOrDefault();
            pavlovServer.RconServer = await rconServerSerivce.FindOne(pavlovServer.RconServerId);
            return pavlovServer;
        }
        public async Task<bool> Upsert(PavlovServer pavlovServer,RconService service,RconServerSerivce rconServerSerivce)
        {
            
            await rconServerSerivce.validateRconServer(pavlovServer,service);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Upsert(pavlovServer);
        }

        public async Task<bool> Delete(long id)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Delete(id);
        }
    }
}