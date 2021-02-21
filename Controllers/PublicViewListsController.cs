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
        private readonly SshServerSerivce _serverService;
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        
        public PublicViewListsController(RconService service,
            SshServerSerivce serverService,
            ServerSelectedMapService serverSelectedMapService,
            MapsService mapsService,
            PavlovServerService pavlovServerService,
            PavlovServerPlayerService pavlovServerPlayerService)
        {
            _service = service;
            _serverService = serverService;
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
                var serverInfo = "";
                try
                {
                    serverInfo = await _service.SendCommand(server, "ServerInfo");
                }
                catch (CommandException e)
                {
                    throw  new PavlovServerPlayerException(e.Message);
                }
            
                var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(serverInfo);
                var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC",""));
                if(map!=null)
                    tmp.ServerInfo.MapPictureLink = map.ImageUrl;
                var model = new PavlovServerPlayerListPublicViewModel()
                {
                    ServerInfo = tmp.ServerInfo,
                    PlayerList = players.Select(x => new PlayerModelExtended()
                    {
                        Cash = x.Cash,
                        KDA = x.KDA,
                        Score = x.Score,
                        TeamId = x.TeamId,
                        UniqueId = x.UniqueId,
                        Username = x.Username
                    }).ToList(),
                    team0Score = tmp.ServerInfo.Team0Score,
                    team1Score = tmp.ServerInfo.Team1Score
                };
                result.Add(model);
            }
            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            return PartialView(result);
        }
    }
}