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
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        
        
        public PavlovServerPlayerService(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService,RconService rconService,PavlovServerInfoService pavlovServerInfoService)
        {
            _rconService = rconService;
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

            if (playersList?.PlayerList.Count>0)
            {
                var infos = await _rconService.GetPlayerInfo(server, playersList.PlayerList.Select(x => x.UniqueId).ToList());
                extendetList = infos;
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