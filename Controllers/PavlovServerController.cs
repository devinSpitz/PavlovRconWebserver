using System.Collections.Generic;
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
        private readonly SshServerSerivce _service;
        private readonly RconService _rconService;
        private readonly UserService _userservice;
        private readonly PavlovServerService _pavlovServerService;        
        private readonly MapsService _mapsService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        
        public PavlovServerController(SshServerSerivce service,UserService userService,PavlovServerService pavlovServerService,RconService rconService,ServerSelectedMapService serverSelectedMapService,MapsService mapsService)
        {
            _service = service;
            _userservice = userService;
            _pavlovServerService = pavlovServerService;
            _rconService = rconService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;

        }
        
        
        [HttpGet("[controller]/EditServer/{serverId}/{sshServerId}")]
        public async Task<IActionResult> EditServer(int serverId,int sshServerId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var server = new PavlovServer();
            if (serverId != 0)
            {
                server = await _pavlovServerService.FindOne(serverId);
            }

            PavlovServerViewModel viewModel = new PavlovServerViewModel();
            viewModel = viewModel.fromPavlovServer(server,sshServerId);
            
            return View("Server",viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditServer(PavlovServerViewModel server)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Server",server);
        }
        
        [HttpGet("[controller]/EditServerSelectedMaps/{serverId}")]
        public async Task<IActionResult> EditServerSelectedMaps(int serverId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var serverSelectedMap = new List<ServerSelectedMap>();
            var server = await _pavlovServerService.FindOne(serverId);
            serverSelectedMap = (await _serverSelectedMapService.FindAllFrom(server)).ToList();

            var tmp = await _mapsService.FindAll();

            var viewModel = new SelectedServerMapsViewModel()
            {
                AllMaps = tmp.ToList(),
                SelectedMaps = serverSelectedMap,
                ServerId = serverId
            };
            return View("ServerMaps",viewModel);
        }
        
        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(PavlovServerViewModel server)
        {
            var newServer = false;
            if(!ModelState.IsValid) 
                return View("Server",server);
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            try
            {
                server.SshServer = await _service.FindOne(server.sshServerId);
                await _pavlovServerService.Upsert(server.toPavlovServer(server), _rconService, _service);
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

            return RedirectToAction("Index","SshServer");
        }
        [HttpGet("[controller]/DeleteServer/{id}")]
        public async Task<IActionResult> DeleteServer(int id)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            await _pavlovServerService.Delete(id);
            return RedirectToAction("Index","SshServer");
        }
    }
}