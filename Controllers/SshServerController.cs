using System;
using System.Collections.Generic;
using System.IO;
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
    public class SshServerController : Controller
    {
        private readonly SshServerSerivce _service;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly RconService _rconService;
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        public SshServerController(SshServerSerivce service,UserService userService,ServerSelectedMapService serverSelectedMapService,RconService rconService,MapsService mapsService,PavlovServerService pavlovServerService)
        {
            _service = service;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _rconService = rconService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
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
            var server = new SshServer();
            if (serverId != null && serverId != 0)
            {
                server = await _service.FindOne((int)serverId);
            }

            try
            {
                server.SshKeyFileNames = Directory.EnumerateFiles("KeyFiles/", "*", SearchOption.AllDirectories)
                    .Select(x => x.Replace("KeyFiles/", "")).ToList();
            }
            catch (Exception e)
            {
                // ignore there is maybe no folder or the folder is empty 
            }
            return View("Server",server);
        }

        [HttpPost]
        public async Task<IActionResult> EditServer(SshServer server)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            try
            {
                server.SshKeyFileNames = Directory.EnumerateFiles("KeyFiles/", "*", SearchOption.AllDirectories)
                    .Select(x => x.Replace("KeyFiles/", "")).ToList();
            }
            catch (Exception e)
            {
                // ignore there is maybe no folder or the folder is empty 
            }
            return View("Server",server);
        }
        
        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(SshServer server)
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
            

            if (newServer) return await Index();
            
            return await Index();
        }

        [HttpGet]
        public async Task<bool> SaveServerSelectedMap(int serverId, string mapId,string gameMode)
        {
            var realMap = await _mapsService.FindOne(mapId);
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            var mapsSelected = await _serverSelectedMapService.FindAllFrom(pavlovServer);
            if (mapsSelected != null)
            {
                var toUpdate = mapsSelected.FirstOrDefault(x => x.Map.Id == realMap.Id);
                if (toUpdate == null)
                {
                   
                    var NewMap = new ServerSelectedMap()
                    {
                        Map = realMap,
                        PavlovServer = pavlovServer
                    };
                    await _serverSelectedMapService.Insert(NewMap); 
                }
                else
                {
                    toUpdate.GameMode = gameMode;
                    await _serverSelectedMapService.Update(toUpdate);
                }
            }
            else
            {
                var NewMap = new ServerSelectedMap()
                {
                    
                    GameMode = gameMode,
                    Map = realMap,
                    PavlovServer = pavlovServer
                };
                await _serverSelectedMapService.Insert(NewMap);
            }
            return true;
        }
        [HttpGet]
        public async Task<bool>  DeleteServerSelectedMap(int serverId, string mapId,string gameMode)
        {
            var realMap = await _mapsService.FindOne(mapId);
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            var mapsSelected = await _serverSelectedMapService.FindAllFrom(pavlovServer);
            var list = mapsSelected.ToList();
            if (mapsSelected != null)
            {
                var toUpdate = mapsSelected.FirstOrDefault(x => x.Map.Id == realMap.Id);
                await _serverSelectedMapService.Delete(toUpdate.Id);
            }
            return true;
        }
        

        
        
        [HttpGet("[controller]/DeleteServer/{id}")]
        public async Task<IActionResult> DeleteServer(int id)
        {
            if(await _userservice.IsUserNotInRole("Admin",HttpContext.User)) return new UnauthorizedResult();
            await _service.Delete(id);
            return await Index();
        }
        
        
    }
}