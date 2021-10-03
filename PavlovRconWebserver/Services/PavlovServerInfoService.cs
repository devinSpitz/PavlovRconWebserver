using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerInfoService
    {
        private readonly IToastifyService _notifyService;
        private readonly ILiteDbIdentityAsyncContext _liteDb;


        public PavlovServerInfoService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerInfo> FindServer(int serverId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .FindOneAsync(x => x.ServerId == serverId);
        }

        public async Task Upsert(PavlovServerInfo pavlovServerInfo)
        {
            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .DeleteManyAsync(x => x.ServerId == pavlovServerInfo.ServerId);

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerInfo>("PavlovServerInfo")
                .InsertAsync(pavlovServerInfo);
        }
    }
}