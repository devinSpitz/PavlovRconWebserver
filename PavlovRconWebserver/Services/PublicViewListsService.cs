using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PublicViewListsService
    {
        private readonly IToastifyService _notifyService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;

        private readonly PavlovServerPlayerService _pavlovServerPlayerService;

        public PublicViewListsService(
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
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
            var model = new PavlovServerPlayerListPublicViewModel
            {
                ServerInfo = serverInfo,
                PlayerList = players.Select(x => new PlayerModelExtended
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