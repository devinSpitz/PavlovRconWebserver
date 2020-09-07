using System.Collections.Generic;
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
        private readonly RconServerSerivce _serverService;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly MapsService _mapsService;
        public MatchMakingController(RconService service,RconServerSerivce serverService,UserService userService,ServerSelectedMapService serverSelectedMapService,MapsService mapsService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
        }
        
   
        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {

            return View(new RconViewModel());
        }

        
    }
}