using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerPlayerHistoryService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public PavlovServerPlayerHistoryService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<PavlovServerPlayerHistory>> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.ServerId == serverId);
        }

        public async Task<IEnumerable<PavlovServerPlayerHistory>> FindAllFromPlayer(string uniqueId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.UniqueId == uniqueId);
        }

        public async Task Upsert(List<PavlovServerPlayerHistory> pavlovServerPlayerHistories, int serverId,
            int deleteAfterDays)
        {
            var toDelete = (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x =>
                    x.ServerId == serverId && x.date.Add(new TimeSpan(deleteAfterDays, 0, 0, 0)) < DateTime.Now);

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .DeleteManyAsync(x => toDelete.Select(y => y.Id).ToList().Contains(x.Id));

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .InsertAsync(pavlovServerPlayerHistories);
        }
    }
}