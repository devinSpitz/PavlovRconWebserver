using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    public class PublicViewListsController : Controller
    {
        
        private readonly RconService _service;
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        
        
        public PublicViewListsController(RconService service,
            PavlovServerInfoService pavlovServerInfoService,
            MapsService mapsService,
            PavlovServerService pavlovServerService,
            PavlovServerPlayerService pavlovServerPlayerService)
        {
            _service = service;
            _pavlovServerInfoService = pavlovServerInfoService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
        }
        
        [HttpGet("[controller]/PlayersFromServers/")]
        // GET
        public async Task<IActionResult> PlayersFromServers([FromQuery]int[] servers,[FromQuery]string backgroundColorHex,[FromQuery]string fontColorHex)
        {
            var result = new List<PavlovServerPlayerListPublicViewModel>();
            foreach (var serverId in servers)
            {
                
                var server = await _pavlovServerService.FindOne(serverId);
                var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
                var serverInfo = await _pavlovServerInfoService.FindServer(serverId);
                
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
                result.Add(model);
            }
            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            return PartialView(result);
        }
    }
}