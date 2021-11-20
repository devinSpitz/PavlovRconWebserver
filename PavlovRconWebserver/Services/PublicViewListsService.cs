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
        private readonly ServerSelectedMapService _serverSelectedMapService;

        public PublicViewListsService(
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService,
            PavlovServerService pavlovServerService,
            ServerSelectedMapService serverSelectedMapService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerService = pavlovServerService;
            _serverSelectedMapService = serverSelectedMapService;
        }


        public async Task<PavlovServerPlayerListPublicViewModel> GetPavlovServerPlayerListPublicViewModel(int serverId,bool withMaps)
        {
            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            var serverInfo = await _pavlovServerInfoService.FindServer(serverId);
            var model = PavlovServerPlayerListPublicViewModel(serverInfo, players);
            if (model == null) return null;
            model.serverId = serverId;
            model.withMaps = withMaps;
            return model;
        }        
        public async Task<PavlovServerPublicMapListViewModel> GetPavlovServerPublicMapListViewModel(int serverId)
        {
            var tmp = new PavlovServerPublicMapListViewModel();
            var server = await _pavlovServerService.FindOne(serverId);
            var serverInfo = await _pavlovServerInfoService.FindServer(serverId);
            var maps = await _serverSelectedMapService.FindAllFrom(server);
            tmp.MapList = maps;
            tmp.ServerInfo = serverInfo;
            return tmp;
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

                var tmp = await GetPavlovServerPlayerListPublicViewModel(server.Id, true);
                tmp.serverId = server.Id;
                result.Add(tmp);
            }


            return result.OrderByDescending(x=>x.PlayerList.Count).ToList();
        }
        
        public async Task<PavlovServerPublicMapListViewModel> GetMapCycleFromPavlovServer()
        {
            var result = new PavlovServerPublicMapListViewModel();
            var servers = (await _pavlovServerService.FindAll()).ToArray();
            foreach (var server in servers)
            {
                if (server == null) continue;
                if (server.ServerServiceState != ServerServiceState.active &&
                    server.ServerType == ServerType.Community) continue;
                if (server.ServerType == ServerType.Event) continue;
                result = await GetPavlovServerPublicMapListViewModel(server.Id);
            }


            return result;
        }

        public PavlovServerPlayerListPublicViewModel PavlovServerPlayerListPublicViewModel(PavlovServerInfo serverInfo,
            IEnumerable<PavlovServerPlayer> players)
        {
            if (serverInfo == null) return null;
            var model = new PavlovServerPlayerListPublicViewModel
            {
                ServerInfo = serverInfo,
                PlayerList = players.Select(x => new PavlovServerPlayer
                {
                    Cash = x.Cash,
                    KDA = x.KDA,
                    Score = x.Score,
                    TeamId = x.TeamId,
                    UniqueId = x.UniqueId,
                    Username = x.Username,
                    Headshot = x.Headshot,
                    Kills = x.Kills,
                    Deaths = x.Deaths,
                    Assists = x.Assists
                    
                }).ToList(),
                team0Score = serverInfo.Team0Score,
                team1Score = serverInfo.Team1Score
            };
            return model;
        }
    }
}