using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [AllowAnonymous]
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

        [HttpGet("[controller]/Index/")]
        public async Task<IActionResult> Index()
        {
            var servers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Community);
            var tmp = new LeaderBoardViewModel()
            {
                server = 0,
                AllServers = servers.Prepend(new PavlovServer()
                {
                    Id = 0,
                    Name = "--Please select--"
                }).ToList(),
                list = Array.Empty<SteamIdentityStatsServerViewModel>()
            };
            return View("Index", tmp);
        }
        [HttpGet("[controller]/Index/Server/")]
        public async Task<IActionResult> Server(int server)
        {
            if(server==0) return RedirectToAction("Index");
            var servers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Community).ToList();
            
            if(!servers.Select(x=>x.Id).Contains(server)) return RedirectToAction("Index");
            var steamIdentityStats = (await _steamIdentityStatsServerService.FindAll()).Where(x=>x.ServerId==server);
            var tmp = new LeaderBoardViewModel()
            {
                server = servers.First(x=>x.Id==server).Id,
                AllServers = servers.Prepend(new PavlovServer()
                {
                    Id = 0,
                    Name = "--Please select--"
                }).ToList(),
                list = steamIdentityStats.Select(x => new SteamIdentityStatsServerViewModel
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
                })
            };
            return View("Index", tmp);
        }
        
        [HttpGet("[controller]/Index/User/{steamId}")]
        public async Task<IActionResult> User(string steamId)
        {
            var servers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Community);
            var steamIdentityStats = (await _steamIdentityStatsServerService.FindAll()).Where(x=>x.SteamId==steamId);
            var tmp = new LeaderBoardViewModel()
            {
                server = 0,
                AllServers = servers.Prepend(new PavlovServer()
                {
                    Id = 0,
                    Name = "--Please select--"
                }).ToList(),
                list = steamIdentityStats.Select(x => new SteamIdentityStatsServerViewModel
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
                })
            };
            return View("Index", tmp);
        }
    }
}