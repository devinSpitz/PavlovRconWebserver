using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public PavlovServerService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }


        public async Task<bool> IsModSomeWhere(LiteDbUser user, ServerSelectedModsService serverSelectedModsService)
        {
            var servers = (await FindAll()).Where(x => x.ServerServiceState == ServerServiceState.active).ToList();
            var isModSomeWhere = false;
            foreach (var pavlovServer in servers)
                if (isModSomeWhere ||
                    await RightsHandler.IsModOnTheServer(serverSelectedModsService, pavlovServer, user.Id))
                    isModSomeWhere = true;

            return isModSomeWhere;
        }

        public async Task<IEnumerable<PavlovServer>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .FindAll().OrderByDescending(x => x.Id);
        }

        public List<PavlovServer> FindAllFrom(int sshServerId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .Find(x => x.SshServer.Id == sshServerId).ToList();
        }

        public async Task<PavlovServer> FindOne(int id)
        {
            var pavlovServer = _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Find(x => x.Id == id).FirstOrDefault();
            return pavlovServer;
        }

        public async Task<bool> Upsert(PavlovServer pavlovServer, RconService service,
            SshServerSerivce sshServerSerivce, bool withCheck = true)
        {
            if (withCheck)
                pavlovServer = await sshServerSerivce.validatePavlovServer(pavlovServer, service);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Upsert(pavlovServer);
        }

        public async Task<bool> Delete(int id,
            ServerSelectedWhitelistService serverSelectedWhiteList,
            ServerSelectedMapService serverSelectedMapService,
            ServerSelectedModsService serverSelectedModsService)
        {
            var server = await FindOne(id);
            await serverSelectedMapService.DeleteFromServer(server);
            await serverSelectedModsService.DeleteFromServer(server);
            await serverSelectedWhiteList.DeleteFromServer(server);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Delete(id);
        }
    }
}