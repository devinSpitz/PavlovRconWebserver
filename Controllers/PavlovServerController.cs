using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    public class PavlovServerController : Controller
    {
        private readonly RconServerSerivce _service;
        private readonly RconService _rconService;
        private readonly UserService _userservice;
        private readonly PavlovServerService _pavlovServerService;        
        private readonly MapsService _mapsService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        
        public PavlovServerController(RconServerSerivce service,UserService userService,PavlovServerService pavlovServerService,RconService rconService,ServerSelectedMapService serverSelectedMapService,MapsService mapsService)
        {
            _service = service;
            _userservice = userService;
            _pavlovServerService = pavlovServerService;
            _rconService = rconService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;

        }
        
        
        [HttpGet("[controller]/EditServer/{serverId}/{rconServerId}")]
        public async Task<IActionResult> EditServer(int serverId,int rconServerId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var server = new PavlovServer();
            if (serverId != null && serverId != 0)
            {
                server = await _pavlovServerService.FindOne(serverId);
            }
            else
            {
                server.RconServerId = rconServerId;
            }
            return View("Server",server);
        }

        [HttpPost]
        public async Task<IActionResult> EditServer(PavlovServer server)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Server",server);
        }
        
        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(PavlovServer server)
        {
            var newServer = false;
            if(!ModelState.IsValid) 
                return View("Server",server);
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            try
            {
                server.RconServer = await _service.FindOne(server.RconServerId);
                await _pavlovServerService.Upsert(server, _rconService, _service);
            }
            catch (SaveServerException e)
            {
                if (e.FieldName == "" && e.Message.ToLower().Contains("telnet"))
                {
                    ModelState.AddModelError("TelnetPassword", e.Message);
                }
                else if (e.FieldName == "")
                {
                    ModelState.AddModelError("Id", e.Message);
                }
                else
                {
                    ModelState.AddModelError(e.FieldName, e.Message);
                }

            }

            if (ModelState.ErrorCount > 0)
            {
                    return await EditServer(server);
            }
            
            return await(new RconServerController(_service,_userservice,_serverSelectedMapService,_rconService,_mapsService).Index());
        }

    }
}