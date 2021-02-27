using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize]
    public class MatchMakingController : Controller
    {
        private readonly RconService _service;
        private readonly SshServerSerivce _serverService;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly MapsService _mapsService;
        private readonly MatchService _matchService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly TeamService _teamService;
        public MatchMakingController(RconService service,SshServerSerivce serverService,UserService userService,ServerSelectedMapService serverSelectedMapService,MapsService mapsService,MatchService matchService,PavlovServerService pavlovServerService,TeamService teamService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _matchService = matchService;
            _pavlovServerService = pavlovServerService;
            _teamService = teamService;
        }


        [HttpGet("[controller]/{showFinished?}")]
        public async Task<IActionResult> Index(bool showFinished = false)
        {
            return showFinished ? View((await _matchService.FindAll()).Where(x=>x.Status!=Status.Finshed)) : View(await _matchService.FindAll());
        }

        
        public async Task<IActionResult> CreateMatch()
        {
            var match = new Match()
            {
                AllTeams = (await _teamService.FindAll()).ToList(),
                AllPavlovServers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Event).ToList() // and where no match is already running
                
            };
            return View("Match",match);
        }
                
        [HttpPost("[controller]/PartialViewPerGameMode")]
        public async Task<IActionResult> PartialViewPerGameMode(string gameMode)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();


            var gotAnswer = GameModes.HasTeams.TryGetValue(gameMode, out var hasTeams);
            if (gotAnswer)
            {
                if (hasTeams)
                {
                    var gotAnswer2 = GameModes.OneTeam.TryGetValue(gameMode, out var oneTeam);
                    if (gotAnswer2)
                    {
                        return PartialView(oneTeam ? "SteamIdentityPartialView" : "TeamPartailView");
                    }
                    
                    BadRequest("internal error!");
                }
                else
                {
                    return PartialView("SteamIdentityPartialView");
                }
            }
            else
            {
                return BadRequest("There is no gameMode like that!");
            }
            return BadRequest("There is no gameMode like that!"); 
        }
        
        
    }
}