using System;
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
    
    [Authorize(Roles = CustomRoles.Admin)]
    public class SshServerController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SshServerSerivce _service;
        private readonly UserService _userservice;

        public SshServerController(SshServerSerivce service, UserService userService,
            ServerSelectedMapService serverSelectedMapService, MapsService mapsService,
            PavlovServerService pavlovServerService)
        {
            _service = service;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
        }

        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            return View("Index", await _service.FindAll());
        }

        [HttpGet("[controller]/EditServer/{serverId?}")]
        public async Task<IActionResult> EditServer(int? serverId)
        {
            var server = new SshServer();
            if (serverId != null && serverId != 0) server = await _service.FindOne((int) serverId);

            try
            {
                server.SshKeyFileNames = Directory.EnumerateFiles("KeyFiles/", "*", SearchOption.AllDirectories)
                    .Select(x => x.Replace("KeyFiles/", "")).ToList();
            }
            catch (Exception)
            {
                // ignore there is maybe no folder or the folder is empty 
            }

            ViewBag.Remove = false;
            return View("Server", server);
        }

        [HttpPost]
        public async Task<IActionResult> EditServer(SshServer server, bool remove = false)
        {
            try
            {
                server.SshKeyFileNames = Directory.EnumerateFiles("KeyFiles/", "*", SearchOption.AllDirectories)
                    .Select(x => x.Replace("KeyFiles/", "")).ToList();
            }
            catch (Exception)
            {
                // ignore there is maybe no folder or the folder is empty 
            }

            ViewBag.Remove = remove;
            return View("Server", server);
        }

        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(SshServer server)
        {
            var newServer = false;
            if (!ModelState.IsValid)
                return View("Server", server);
            try
            {
                if (server.Id == 0)
                {
                    newServer = true;
                    server.Id = await _service.Insert(server);
                }
                else
                {
                    await _service.Update(server);
                }
            }
            catch (SaveServerException e)
            {
                if (e.FieldName == "" && e.Message.ToLower().Contains("telnet"))
                    ModelState.AddModelError("Password", e.Message);
                else if (e.FieldName == "" && e.Message.ToLower().Contains("ssh"))
                    ModelState.AddModelError("SshPassword", e.Message);
                else if (e.FieldName == "")
                    ModelState.AddModelError("Id", e.Message);
                else
                    ModelState.AddModelError(e.FieldName, e.Message);
            }

            if (ModelState.ErrorCount > 0)
            {
                if (newServer)
                    return await EditServer(server);
                return await EditServer(server);
            }


            if (newServer) return await Index();

            return await Index();
        }

        [HttpGet]
        public async Task<bool> SaveServerSelectedMap(int serverId, string mapId, string gameMode)
        {
            var realMap = await _mapsService.FindOne(mapId);
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            var mapsSelected = await _serverSelectedMapService.FindAllFrom(pavlovServer);
            if (mapsSelected != null)
            {
                var toUpdate = mapsSelected.FirstOrDefault(x => x.Map.Id == realMap.Id && x.GameMode == gameMode);
                if (toUpdate == null)
                {
                    var newMap = new ServerSelectedMap
                    {
                        Map = realMap,
                        PavlovServer = pavlovServer,
                        GameMode = gameMode
                    };
                    await _serverSelectedMapService.Insert(newMap);
                }
                else
                {
                    toUpdate.GameMode = gameMode;
                    await _serverSelectedMapService.Update(toUpdate);
                }
            }
            else
            {
                var newMap = new ServerSelectedMap
                {
                    GameMode = gameMode,
                    Map = realMap,
                    PavlovServer = pavlovServer
                };
                await _serverSelectedMapService.Insert(newMap);
            }

            return true;
        }

        [HttpGet]
        public async Task<bool> DeleteServerSelectedMap(int serverId, string mapId, string gameMode)
        {
            var realMap = await _mapsService.FindOne(mapId);
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            var mapsSelected = await _serverSelectedMapService.FindAllFrom(pavlovServer);
            if (mapsSelected != null)
            {
                var toUpdate = mapsSelected.FirstOrDefault(x => x.Map.Id == realMap.Id && x.GameMode == gameMode);
                if (toUpdate != null) await _serverSelectedMapService.Delete(toUpdate.Id);
            }

            return true;
        }


        [HttpGet("[controller]/DeleteServer/{id}")]
        public async Task<IActionResult> DeleteServer(int id)
        {
            await _service.Delete(id);
            return await Index();
        }
    }
}