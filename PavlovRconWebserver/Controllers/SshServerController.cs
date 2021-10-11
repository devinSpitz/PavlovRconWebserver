using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize(Roles = CustomRoles.OnPremiseOrRent)]
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
            var user = await _userservice.getUserFromCp(HttpContext.User);
            return View("Index", await _service.FindAllWithRightsCheck(HttpContext.User, user));
        }

        [Authorize(Roles = CustomRoles.OnPremise)]
        [HttpGet("[controller]/EditServer/{serverId?}")]
        public async Task<IActionResult> EditServer(int? serverId)
        {
            if (serverId!= null && !await CheckIfUserHasrights((int) serverId)) return Forbid();
            var server = new SshServer();
            if (serverId != null && serverId != 0) server = await _service.FindOne((int) serverId);
            if(server.Id==0)
                if (!HttpContext.User.IsInRole("Admin"))
                    return Forbid();
            ViewBag.Remove = false;;
            server.LiteDbUsers = (await _userservice.FindAll()).ToList();
            return View("Server", server);
        }

        [Authorize(Roles = CustomRoles.OnPremise)]
        [HttpPost]
        public async Task<IActionResult> EditServer(SshServer server, bool remove = false)
        {
            if (!await CheckIfUserHasrights(server.Id)) return Forbid();

            ViewBag.Remove = remove;
            
            server.LiteDbUsers = (await _userservice.FindAll()).ToList();
            return View("Server", server);
        }

        [Authorize(Roles = CustomRoles.OnPremise)]
        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(SshServer server)
        {
            if (!await CheckIfUserHasrights(server.Id)) return Forbid();
            var newServer = false;
            if (!ModelState.IsValid)
                return View("Server", server);

            server.LiteDbUsers = (await _userservice.FindAll()).ToList();
            if (server.SshKeyFileNameForm != null)
            {
                await using var ms = new MemoryStream();
                await server.SshKeyFileNameForm.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                server.SshKeyFileName = fileBytes;
                // act on the Base64 data
            }
            
            server.Owner = (await _userservice.FindAll())
                .FirstOrDefault(x => x.Id == new ObjectId(server.LiteDbUserId));
            try
            {
                if (server.Id == 0)
                {
                    newServer = true;
                    server.Id = await _service.Insert(server);
                }
                else
                {
                    if (server.SshKeyFileName == null)
                    {
                        var old = await _service.FindOne(server.Id);
                        if (old.SshKeyFileName != null)
                        {
                            server.SshKeyFileName = old.SshKeyFileName;
                        }
                    }
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

        [HttpGet("[controller]/SaveServerSelectedMap")]
        public async Task<bool> SaveServerSelectedMap(int serverId, string mapId, string gameMode)
        {
            if (!await RightsHandler.HasRightsToThisPavlovServer(HttpContext.User,
                await _userservice.getUserFromCp(HttpContext.User), serverId, _service, _pavlovServerService))
                return false;
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

        [HttpGet("[controller]/DeleteServerSelectedMap")]
        public async Task<bool> DeleteServerSelectedMap(int serverId, string mapId, string gameMode)
        {  
            if (!await RightsHandler.HasRightsToThisPavlovServer(HttpContext.User,
                await _userservice.getUserFromCp(HttpContext.User), serverId, _service, _pavlovServerService))
                return false;
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
            if (!await CheckIfUserHasrights(id)) return Forbid();
            await _service.Delete(id);
            return await Index();
        }

        private async Task<bool> CheckIfUserHasrights(int id)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            if (await RightsHandler.HasRightsToThisSshServer(HttpContext.User, user, id, _service)) return true;

            return false;
        }
    }
}