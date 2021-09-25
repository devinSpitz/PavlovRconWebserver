using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerPlayerHistoryService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;


        public PavlovServerPlayerHistoryService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerPlayerHistory[]> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.ServerId == serverId).ToArray();
        }

        public async Task<PavlovServerPlayerHistory[]> FindAllFromPlayer(string uniqueId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.UniqueId == uniqueId).ToArray();
        }

        public async Task Upsert(List<PavlovServerPlayerHistory> pavlovServerPlayerHistories, int serverId,
            int deleteAfterDays)
        {
            var toDelete = (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x =>
                    x.ServerId == serverId && x.date.Add(new TimeSpan(deleteAfterDays, 0, 0, 0)) < DateTime.Now);

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .DeleteManyAsync(x => toDelete.Select(y => y.Id).ToArray().Contains(x.Id));

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .InsertAsync(pavlovServerPlayerHistories);
        }
    }
}