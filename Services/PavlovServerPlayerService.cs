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
        private readonly RconService _rconService;
        
        
        public PavlovServerPlayerService(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService,RconService rconService)
        {
            _rconService = rconService;
            _pavlovServerService = pavlovServerService;
            _liteDb = liteDbContext;
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

        
        public async Task<bool> SaveRealTimePlayerListFromServer(int serverId)
        {
            var server = await _pavlovServerService.FindOne(serverId);
            var playersTmp = "";
            var extendetList = new List<PlayerModelExtended>();
            // need to get the live info
            PlayerListClass playersList = new PlayerListClass();
            try
            {
                playersTmp = await _rconService.SendCommand(server, "RefreshList");
            }
            catch (CommandException e)
            {
                throw  new PavlovServerPlayerException(e.Message);
            }
            playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);

            if (playersList.PlayerList != null)
            {
                int i = 0;
                var query = from s in playersList.PlayerList 
                    let num = i++
                    group s by num / 3 into g
                    select g.ToArray();
                var playerGroups = query.ToArray();
                
                foreach (var playerGroup in playerGroups)
                {
                    extendetList.AddRange(await Task.WhenAll(playerGroup
                        .Select(i => _rconService.GetPlayerInfo(server, i.UniqueId, i.Username))
                        .ToArray()));
                }
            }

            var pavlovServerPlayerList = extendetList.Select(x => new PavlovServerPlayer
            {
                Username = x.Username,
                UniqueId = x.UniqueId,
                KDA = x.KDA,
                Cash = x.Cash,
                TeamId = x.TeamId,
                Score = x.Score,
                ServerId = serverId
            }).ToList();
            var playersFound = await Upsert(pavlovServerPlayerList, serverId);
            return true;
        }
        
        
    }
}