using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    public class PublicViewListsController : Controller
    {
        
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly UserService _userService;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        
        
        public PublicViewListsController(PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,
            UserService userService,
            PavlovServerInfoService pavlovServerInfoService,
            PavlovServerPlayerService pavlovServerPlayerService)
        {
            _pavlovServerInfoService = pavlovServerInfoService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _userService = userService;
        }
        
        [HttpGet("[controller]/PlayersFromServers/")]
        // GET
        public async Task<IActionResult> PlayersFromServers([FromQuery]int[] servers,[FromQuery]string backgroundColorHex,[FromQuery]string fontColorHex)
        {
            var result = new List<PavlovServerPlayerListPublicViewModel>();
            foreach (var serverId in servers)
            {
                
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
        
        [HttpGet("[controller]/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayer(string uniqueId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))  return BadRequest("You need to be admin!");
            return View("PlayersHistory",(await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToList());
        }
        
        [HttpGet("[controller]/API/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayerApi(string uniqueId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))  return BadRequest("You need to be admin!");
            return new ObjectResult((await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToList());
        }

        [HttpGet("[controller]/GetHistoryOfServer/{serverId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfServer(int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))  return BadRequest("You need to be admin!");
            return View("PlayersHistory",(await _pavlovServerPlayerHistoryService.FindAllFromServer(serverId))?.ToList());
        }
        
        [HttpGet("[controller]/API/GetHistoryOfServer/{serverId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfServerApi(int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))  return BadRequest("You need to be admin!");
            return new ObjectResult((await _pavlovServerPlayerHistoryService.FindAllFromServer(serverId))?.ToList());
        }
    }
    

}