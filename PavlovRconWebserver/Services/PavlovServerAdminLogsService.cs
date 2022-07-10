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
    public class PavlovServerAdminLogsService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;


        public PavlovServerAdminLogsService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerAdminLogs[]> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<PavlovServerAdminLogs>("PavlovServerAdminLogs")
                .FindAllAsync()).Where(x => x.ServerId == serverId).ToArray();
        }    
        
        public async Task<int> DeleteMany(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<PavlovServerAdminLogs>("PavlovServerAdminLogs")
                .DeleteManyAsync(x=>x.ServerId==serverId));
        }
        

        public async Task Upsert(List<PavlovServerAdminLogs> pavlovServerPlayerHistories)
        {
            await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerAdminLogs>("PavlovServerAdminLogs")
                .InsertAsync(pavlovServerPlayerHistories);
        }
    }
}