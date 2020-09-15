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
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.RconServer)
                .FindAll().OrderByDescending(x=>x.Id);
        }
        
        public List<PavlovServer> FindAllFrom(int rconServerId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.RconServer)
                .Find(x=>x.RconServer.Id==rconServerId).ToList();
        }
        public async Task<PavlovServer> FindOne(long id)
        {
            var pavlovServer =  _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.RconServer)
                .Find(x => x.Id == id).FirstOrDefault();
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