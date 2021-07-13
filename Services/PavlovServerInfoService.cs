using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerInfoService
    {
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;

        private readonly ILiteDbIdentityContext _liteDb;


        public PavlovServerInfoService(ILiteDbIdentityContext liteDbContext, PavlovServerService pavlovServerService,
            MapsService mapsService)
        {
            _pavlovServerService = pavlovServerService;
            _mapsService = mapsService;
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerInfo> FindServer(int serverId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .Find(x => x.ServerId == serverId).FirstOrDefault();
        }

        public async Task Upsert(PavlovServerInfo pavlovServerInfo)
        {
            _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .DeleteMany(x => x.ServerId == pavlovServerInfo.ServerId);

            _liteDb.LiteDatabase.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .Insert(pavlovServerInfo);
        }
    }
}