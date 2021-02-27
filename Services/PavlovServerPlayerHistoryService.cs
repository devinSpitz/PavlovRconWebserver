using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerPlayerHistoryService
    {
        
        private ILiteDbIdentityContext _liteDb;
        
        
        public PavlovServerPlayerHistoryService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<PavlovServerPlayerHistory>> FindAllFromServer(int serverId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAll().Where(x => x.ServerId == serverId);
        }
        
        public async Task<IEnumerable<PavlovServerPlayerHistory>> FindAllFromPlayer(string uniqueId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAll().Where(x => x.UniqueId == uniqueId);
        }

        public async Task Upsert(List<PavlovServerPlayerHistory> pavlovServerPlayerHistories,int serverId,int deleteAfterDays)
        {
            var toDelete = _liteDb.LiteDatabase.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .FindAll().Where(x=>x.ServerId == serverId&&(x.date.Add(new TimeSpan(deleteAfterDays,0,0,0)))<DateTime.Now);

            _liteDb.LiteDatabase.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .DeleteMany(x=>toDelete.Select(y=>y.Id).ToList().Contains(x.Id));
            
            _liteDb.LiteDatabase.GetCollection<PavlovServerPlayerHistory>("PavlovServerPlayerHistory")
                .Insert(pavlovServerPlayerHistories);
        }
        
        
        
    }
}