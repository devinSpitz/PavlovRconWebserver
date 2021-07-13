using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    public class PublicViewListsController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly MatchService _matchService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        private readonly PavlovServerService _pavlovServerService;

        private readonly PublicViewListsService _publicViewListsService;
        private readonly UserService _userService;

        public PublicViewListsController(PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,
            UserService userService, PavlovServerService pavlovServerService,
            PublicViewListsService publicViewListsService, MatchService matchService, MapsService mapsService)
        {
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _userService = userService;
            _pavlovServerService = pavlovServerService;
            _publicViewListsService = publicViewListsService;
            _matchService = matchService;
            _mapsService = mapsService;
        }

        [HttpGet("[controller]/PlayersFromServers/")]
        // GET
        public async Task<IActionResult> PlayersFromServers([FromQuery] int[] servers,
            [FromQuery] string backgroundColorHex, [FromQuery] string fontColorHex)
        {
            var result = new List<PavlovServerPlayerListPublicViewModel>();
            foreach (var serverId in servers)
            {
                var server = await _pavlovServerService.FindOne(serverId);
                if (server == null) continue;
                if (server.ServerServiceState != ServerServiceState.active &&
                    server.ServerType == ServerType.Community) continue;
                if (server.ServerType == ServerType.Event) continue;
                result.Add(await _publicViewListsService.GetPavlovServerPlayerListPublicViewModel(serverId));
            }

            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            return PartialView(result);
        }

        [HttpGet("[controller]/PlayersFromMatches/")]
        // GET
        public async Task<IActionResult> PlayersFromMatches([FromQuery] int[] matchIds,
            [FromQuery] string backgroundColorHex, [FromQuery] string fontColorHex)
        {
            var result = new List<PavlovServerPlayerListPublicViewModel>();
            foreach (var matchId in matchIds)
            {
                var match = await _matchService.FindOne(matchId);
                if (match == null) continue;
                if (!match.hasStats()) continue;
                var map = await _mapsService.FindOne(match.MapId.Replace("UGC", ""));
                if (map == null) continue;
                result.Add(_publicViewListsService.PavlovServerPlayerListPublicViewModel(new PavlovServerInfo
                {
                    MapLabel = match.MapId,
                    MapPictureLink = map.ImageUrl,
                    GameMode = match.EndInfo.GameMode,
                    ServerName = match.EndInfo.ServerName,
                    RoundState = match.EndInfo.RoundState,
                    PlayerCount = match.EndInfo.PlayerCount,
                    Teams = match.EndInfo.Teams,
                    Team0Score = match.EndInfo.Team0Score,
                    Team1Score = match.EndInfo.Team1Score,
                    ServerId = match.PavlovServer.Id
                }, match.PlayerResults));
            }

            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            return PartialView("PlayersFromServers", result);
        }

        [HttpGet("[controller]/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayer(string uniqueId)
        {
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))
                return BadRequest("You need to be admin!");
            return View("PlayersHistory",
                (await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToList());
        }

        [HttpGet("[controller]/API/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayerApi(string uniqueId)
        {
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))
                return BadRequest("You need to be admin!");
            return new ObjectResult((await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToList());
        }

        [HttpGet("[controller]/GetHistoryOfServer/{serverId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfServer(int serverId)
        {
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))
                return BadRequest("You need to be admin!");
            return View("PlayersHistory",
                (await _pavlovServerPlayerHistoryService.FindAllFromServer(serverId))?.ToList());
        }

        [HttpGet("[controller]/API/GetHistoryOfServer/{serverId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfServerApi(int serverId)
        {
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService))
                return BadRequest("You need to be admin!");
            return new ObjectResult((await _pavlovServerPlayerHistoryService.FindAllFromServer(serverId))?.ToList());
        }
    }
}