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
        private readonly PavlovServerService _pavlovServerService;

        public PublicViewListsService(
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            PavlovServerService pavlovServerService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerService = pavlovServerService;
        }


        public async Task<PavlovServerPlayerListPublicViewModel> GetPavlovServerPlayerListPublicViewModel(int serverId)
        {
            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            var serverInfo = await _pavlovServerInfoService.FindServer(serverId);
            var model = PavlovServerPlayerListPublicViewModel(serverInfo, players);
            return model;
        }        
        
        public async Task<List<PavlovServerPlayerListPublicViewModel>> GetAllStatsFromAllPavlovServers()
        {
            var result = new List<PavlovServerPlayerListPublicViewModel>();
            var servers = (await _pavlovServerService.FindAll()).ToArray();
            foreach (var server in servers)
            {
                if (server == null) continue;
                if (server.ServerServiceState != ServerServiceState.active &&
                    server.ServerType == ServerType.Community) continue;
                if (server.ServerType == ServerType.Event) continue;
                result.Add(await GetPavlovServerPlayerListPublicViewModel(server.Id));
            }


            return result;
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