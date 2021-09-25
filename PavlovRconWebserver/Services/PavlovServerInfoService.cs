using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerInfoService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;


        public PavlovServerInfoService(ILiteDbIdentityAsyncContext liteDbContext)
        {
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