using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using Microsoft.AspNetCore.Authorization;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    [Authorize]
    public class PavlovServerPlayerHistoryService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;


        public PavlovServerPlayerHistoryService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerPlayerHistory[]> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.ServerId == serverId).ToArray();
        }

        public async Task<PavlovServerPlayerHistory[]> FindAllFromPlayer(string uniqueId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x => x.UniqueId == uniqueId).ToArray();
        }

        public async Task Upsert(List<PavlovServerPlayerHistory> pavlovServerPlayerHistories, int serverId,
            int deleteAfterDays)
        {
            var toDelete = (await _liteDb.LiteDatabaseAsync
                .GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAllAsync()).Where(x =>
                x.ServerId == serverId && x.date.Add(new TimeSpan(deleteAfterDays, 0, 0, 0)) < DateTime.Now);

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .DeleteManyAsync(x => toDelete.Select(y => y.Id).ToArray().Contains(x.Id));

            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .InsertAsync(pavlovServerPlayerHistories);
        }
    }
}