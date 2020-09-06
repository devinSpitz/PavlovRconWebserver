using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    public class RconServerController : Controller
    {
        private readonly RconServerSerivce _service;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly RconService _rconService;
        private readonly MapsService _mapsService;
        public RconServerController(RconServerSerivce service,UserService userService,ServerSelectedMapService serverSelectedMapService,RconService rconService,MapsService mapsService)
        {
            _service = service;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _rconService = rconService;
            _mapsService = mapsService;
        }
        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Index",await _service.FindAll());
        }
        [HttpGet("[controller]/EditServer/{serverId?}")]
        public async Task<IActionResult> EditServer(int? serverId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var server = new RconServer();
            if (serverId != null && serverId != 0)
            {
                server = await _service.FindOne((int)serverId);
            }

            
            return View("Server",server);
        }

        [HttpGet]
        public async Task<IActionResult> EditServer(RconServer server)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            return View("Server",server);
        }
        
        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(RconServer server)
        {
            var newServer = false;
            if(!ModelState.IsValid) 
                return View("Server",server);
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            try
            {

                if (server.Id == 0)
                {
                    newServer = true;
                    server.Id =  await _service.Insert(server,_rconService);
                }
                else
                {
                    await _service.Update(server,_rconService);
                }
            }
            catch (SaveServerException e)
            {
                if (e.FieldName == "" && e.Message.ToLower().Contains("telnet"))
                {
                    ModelState.AddModelError("Password", e.Message);
                }
                else if (e.FieldName == "" && e.Message.ToLower().Contains("ssh"))
                {
                    ModelState.AddModelError("SshPassword", e.Message);
                }
                else if (e.FieldName == "")
                {
                    ModelState.AddModelError("Id", e.Message);
                }else 
                {
                    ModelState.AddModelError(e.FieldName, e.Message);
                }
                    
            }

            if (ModelState.ErrorCount > 0)
            {
                if (newServer)
                {
                    return await EditServer(server);
                }
                else
                {
                    return await EditServer(server);
                }
            }
            

            if (newServer) return await EditServerSelectedMaps(server.Id);
            
            return await Index();
        }

        [HttpGet]
        public bool SaveServerSelectedMap(int serverId, string mapId)
        {
            var map = _serverSelectedMapService.FindSelectedMap(serverId, mapId);
            if (map != null) return true;
            var NewMap = new ServerSelectedMap()
            {
                MapId = mapId,
                RconServerId = serverId
            };
            _serverSelectedMapService.Insert(NewMap);
            return true;
        }
        [HttpGet]
        public bool DeleteServerSelectedMap(int serverId, string mapId)
        {
            var map = _serverSelectedMapService.FindSelectedMap(serverId, mapId);
            if (map == null) return true;
            _serverSelectedMapService.Delete(map.Id);
            return true;
        }
        
        [HttpGet("[controller]/EditServerSelectedMaps/{serverId}")]
        public async Task<IActionResult> EditServerSelectedMaps(int serverId)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            var serverSelectedMap = new List<ServerSelectedMap>();
            var server = await _service.FindOne(serverId);
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
        
        
        [HttpGet("[controller]/DeleteServer/{id}")]
        public async Task<IActionResult> DeleteServer(int id)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            _service.Delete(id);
            return await Index();
        }
        
        
    }
}