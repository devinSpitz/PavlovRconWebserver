using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreHtmlToImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PavlovRconWebserver.Extensions.CC.Web.Helpers;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    public class PublicViewListsController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly MatchService _matchService;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        private readonly PavlovServerAdminLogsService _pavlovServerAdminLogsService;
        private readonly PavlovServerService _pavlovServerService;

        private readonly PublicViewListsService _publicViewListsService;
        private readonly UserService _userService;

        public PublicViewListsController(PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,
            UserService userService, PavlovServerService pavlovServerService,PavlovServerAdminLogsService pavlovServerAdminLogsService,
            PublicViewListsService publicViewListsService, MatchService matchService, MapsService mapsService)
        {
            _pavlovServerPlayerHistoryService = pavlovServerPlayerHistoryService;
            _pavlovServerAdminLogsService = pavlovServerAdminLogsService;
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
                result.Add(await _publicViewListsService.GetPavlovServerPlayerListPublicViewModel(serverId,false));
            }

            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            return PartialView(result);
        }    
        [HttpGet("[controller]/PlayersFromServersAsImage/")]
        // GET
        public async Task<IActionResult> PlayersFromServersAsImage([FromQuery] int[] servers,
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
                result.Add(await _publicViewListsService.GetPavlovServerPlayerListPublicViewModel(serverId,false));
            }

            ViewBag.background = backgroundColorHex;
            ViewBag.textColor = fontColorHex;
            ViewBag.bigger = true;
            var partialViewHtml = await this.RenderViewAsync("PlayersFromServers", result, true);
            var converter = new HtmlConverter();
            var bytes = converter.FromHtmlString(partialViewHtml,512,ImageFormat.Png);
           
            return base.File(
                bytes,
                "image/png");
        }        
        
        
        [HttpGet("[controller]/MapFromSerer/{serverId}")]
        // GET
        public async Task<IActionResult> MapFromSerer(int serverId)
        {
            var result = new PavlovServerPublicMapListViewModel();
            var server = await _pavlovServerService.FindOne(serverId);
            if (server == null) BadRequest();
            result = await _publicViewListsService.GetPavlovServerPublicMapListViewModel(serverId);

            return PartialView("MapFromServer",result);
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

        [Authorize(Roles = CustomRoles.Admin)]
        [HttpGet("[controller]/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayer(string uniqueId)
        {
            return View("PlayersHistory",
                (await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToList());
        }

        [Authorize(Roles = CustomRoles.Admin)]
        [HttpGet("[controller]/API/GetHistoryOfPlayer/{uniqueId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfPlayerApi(string uniqueId)
        {
            return new ObjectResult((await _pavlovServerPlayerHistoryService.FindAllFromPlayer(uniqueId))?.ToArray());
        }

        [Authorize(Roles = CustomRoles.OnPremiseOrRent)]
        [HttpGet("[controller]/GetHistoryOfServer/{serverId}")]
        // GET
        public async Task<IActionResult> GetHistoryOfServer(int serverId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var server = await _pavlovServerService.FindAllServerWhereTheUserHasRights(HttpContext.User, user);
            if (!server.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }

            return View("PlayersHistory",
                (await _pavlovServerPlayerHistoryService.FindAllFromServer(serverId))?.ToList());


        }
        
        [Authorize(Roles = CustomRoles.OnPremiseOrRent)]
        [HttpGet("[controller]/GetAdminCommandsLogsHistoryOf/{serverId}")]
        // GET
        public async Task<IActionResult> GetAdminCommandsLogsHistoryOf(int serverId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var server = await _pavlovServerService.FindAllServerWhereTheUserHasRights(HttpContext.User, user);
            if (!server.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
            return View("AdminsLogsHistory",
                (await _pavlovServerAdminLogsService.FindAllFromServer(serverId))?.ToList().OrderByDescending(x=>x.Time).ToList());
        }        
        
        [Authorize(Roles = CustomRoles.OnPremiseOrRent)]
        [HttpGet("[controller]/RemoveAdminCommandsLogsHistoryOf/{serverId}")]
        // GET
        public async Task<IActionResult> RemoveAdminCommandsLogsHistoryOf(int serverId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var server = await _pavlovServerService.FindAllServerWhereTheUserHasRights(HttpContext.User, user);
            if (!server.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }

            await _pavlovServerAdminLogsService.DeleteMany(serverId);
            
            return View("AdminsLogsHistory",
                (await _pavlovServerAdminLogsService.FindAllFromServer(serverId))?.ToList());
        }

    }
}