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
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.SshServer)
                .FindAll().OrderByDescending(x=>x.Id);
        }
        
        public List<PavlovServer> FindAllFrom(int sshServerId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.SshServer)
                .Find(x=>x.SshServer.Id==sshServerId).ToList();
        }
        public async Task<PavlovServer> FindOne(int id)
        {
            var pavlovServer =  _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x=>x.SshServer)
                .Find(x => x.Id == id).FirstOrDefault();
            return pavlovServer;
        }
        public async Task<bool> Upsert(PavlovServer pavlovServer,RconService service,SshServerSerivce sshServerSerivce,bool withCheck = true)
        {
            if(withCheck)
                pavlovServer = await sshServerSerivce.validateSshServer(pavlovServer,service);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Upsert(pavlovServer);
        }

        public async Task<bool> Delete(long id)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Delete(id);
        }
    }
}