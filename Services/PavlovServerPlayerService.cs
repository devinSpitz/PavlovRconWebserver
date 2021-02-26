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
    public class PavlovServerPlayerService
    {
        
        private ILiteDbIdentityContext _liteDb;
        private readonly PavlovServerService _pavlovServerService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        
        
        public PavlovServerPlayerService(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService,PavlovServerInfoService pavlovServerInfoService)
        {
            _pavlovServerService = pavlovServerService;
            _liteDb = liteDbContext;
            _pavlovServerInfoService = pavlovServerInfoService;
        }

        public async Task<IEnumerable<PavlovServerPlayer>> FindAllFromServer(int serverId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .FindAll().Where(x => x.ServerId == serverId);
        }

        public async Task<int> Upsert(List<PavlovServerPlayer> pavlovServerPlayers,int serverId)
        {
            var deletedPlayers = _liteDb.LiteDatabase.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .DeleteMany(x => x.ServerId == serverId);
            var savedPlayers =  _liteDb.LiteDatabase.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .Insert(pavlovServerPlayers);

            return savedPlayers;
        }
        
        
        
    }
}