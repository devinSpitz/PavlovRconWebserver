using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize(Roles = CustomRoles.Admin)]
    public class LeaderBoardController : Controller
    {
        private readonly SteamIdentityStatsServerService _steamIdentityStatsServerService;
        private readonly PavlovServerService _pavlovServerService;

        public LeaderBoardController(SteamIdentityStatsServerService steamIdentityStatsServerService,
            PavlovServerService pavlovServerService)
        {
            _steamIdentityStatsServerService = steamIdentityStatsServerService;
            _pavlovServerService = pavlovServerService;
        }

        public async Task<IActionResult> Index()
        {
            var servers = await _pavlovServerService.FindAll();
            var steamIdentityStats = await _steamIdentityStatsServerService.FindAll();
            var viewModels = steamIdentityStats.Select(x => new SteamIdentityStatsServerViewModel
            {
                SteamId = x.SteamId,
                SteamName = x.SteamName,
                SteamPicture = x.SteamPicture,
                serverName = servers.First(y=>y.Id==x.ServerId).Name,
                Kills = x.Kills,
                Deaths = x.Deaths,
                Assists = x.Assists,
                Exp = x.Exp,
                UpTime = x.UpTime,
            });
            return View("Index", viewModels);
        }
    }
}