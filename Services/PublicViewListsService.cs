using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Services
{
    public class PublicViewListsService : Controller
    {
        
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        
        public PublicViewListsService(
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService)
        {
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
        }
        


        public async Task<PavlovServerPlayerListPublicViewModel> GetPavlovServerPlayerListPublicViewModel(int serverId)
        {
            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            var serverInfo = await _pavlovServerInfoService.FindServer(serverId);
            var model = PavlovServerPlayerListPublicViewModel(serverInfo, players);
            return model;
        }

        public PavlovServerPlayerListPublicViewModel PavlovServerPlayerListPublicViewModel(PavlovServerInfo serverInfo,
            IEnumerable<PavlovServerPlayer> players)
        {
            var model = new PavlovServerPlayerListPublicViewModel()
            {
                ServerInfo = serverInfo,
                PlayerList = players.Select(x => new PlayerModelExtended()
                {
                    Cash = x.Cash,
                    KDA = x.KDA,
                    Score = x.Score,
                    TeamId = x.TeamId,
                    UniqueId = x.UniqueId,
                    Username = x.Username
                }).ToList(),
                team0Score = serverInfo.Team0Score,
                team1Score = serverInfo.Team1Score
            };
            return model;
        }
    }
    

}