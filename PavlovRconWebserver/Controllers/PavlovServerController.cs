using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize(Roles = CustomRoles.Admin)]
    public class PavlovServerController : Controller
    {
        private readonly IToastifyService _notifyService;
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly RconService _rconService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly SshServerSerivce _service;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserService _userservice;
        private readonly ServerSelectedWhitelistService _whitelistService;
        private readonly UserManager<LiteDbUser> UserManager;


        public PavlovServerController(SshServerSerivce service,
            UserService userService,
            PavlovServerService pavlovServerService,
            RconService rconService,
            ServerSelectedMapService serverSelectedMapService,
            MapsService mapsService,
            ServerSelectedWhitelistService whitelistService,
            ServerSelectedModsService serverSelectedModsService,
            SteamIdentityService steamIdentityService,
            UserManager<LiteDbUser> userManager,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _service = service;
            _userservice = userService;
            _pavlovServerService = pavlovServerService;
            _rconService = rconService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _whitelistService = whitelistService;
            _steamIdentityService = steamIdentityService;
            _serverSelectedModsService = serverSelectedModsService;
            UserManager = userManager;
        }


        [HttpGet("[controller]/EditServer/{serverId}/{sshServerId}/{create?}/{remove?}")]
        public async Task<IActionResult> EditServer(int serverId, int sshServerId, bool create = false,
            bool remove = false)
        {
            var server = new PavlovServer();
            if (serverId != 0) server = await _pavlovServerService.FindOne(serverId);

            var viewModel = new PavlovServerViewModel();
            viewModel = viewModel.fromPavlovServer(server, sshServerId);

            try
            {
                viewModel.SshKeyFileNames = Directory.EnumerateFiles("KeyFiles/", "*", SearchOption.AllDirectories)
                    .Select(x => x.Replace("KeyFiles/", "")).ToList();
            }
            catch (Exception)
            {
                // ignore there is maybe no folder or the folder is empty 
            }

            viewModel.create = create;
            viewModel.remove = remove;
            return View("Server", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditServer(PavlovServerViewModel server)
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

            return View("Server", server);
        }

        [HttpGet("[controller]/EditServerSelectedMaps/{serverId}")]
        public async Task<IActionResult> EditServerSelectedMaps(int serverId)
        {
            var serverSelectedMap = new List<ServerSelectedMap>();
            var server = await _pavlovServerService.FindOne(serverId);
            serverSelectedMap = (await _serverSelectedMapService.FindAllFrom(server)).ToList();

            var tmp = await _mapsService.FindAll();

            var viewModel = new SelectedServerMapsViewModel
            {
                AllMaps = tmp.ToList(),
                SelectedMaps = serverSelectedMap,
                ServerId = serverId
            };
            return View("ServerMaps", viewModel);
        }

        [HttpPost("[controller]/SaveServer")]
        public async Task<IActionResult> SaveServer(PavlovServerViewModel server)
        {
            if (!ModelState.IsValid)
                return View("Server", server);

            var resultServer = new PavlovServer();
            try
            {
                server.SshServer = await _service.FindOne(server.sshServerId);
                
                if (server.create)
                    try
                    {
                        try
                        {
                            await _pavlovServerService.IsValidOnly(server, false);

                            if (string.IsNullOrEmpty(server.SshServer.SshPassword))
                                throw new SaveServerException("Id",
                                    "Please add a sshPassword to the ssh user (Not root user). Sometimes the systems asks for the password even if the keyfile and passphrase is used.");
                            if (string.IsNullOrEmpty(server.ServerFolderPath))
                                throw new SaveServerException("ServerFolderPath",
                                    "The server ServerFolderPath is needed!");
                            if (server.ServerPort <= 0)
                                throw new SaveServerException("ServerPort", "The server port is needed!");
                            if (server.TelnetPort <= 0)
                                throw new SaveServerException("TelnetPort", "The rcon port is needed!");
                            if (string.IsNullOrEmpty(server.ServerSystemdServiceName))
                                throw new SaveServerException("ServerSystemdServiceName",
                                    "The server service name is needed!");
                            if (string.IsNullOrEmpty(server.Name))
                                throw new SaveServerException("Name", "The Gui name is needed!");
                        }
                        catch (SaveServerException e)
                        {
                            return await GoBackEditServer(server,
                                "Field is not set: " + e.Message);
                        }
                        var result = await _pavlovServerService.CreatePavlovServer(server);
                        server = result.Key;
                        if (result.Value != null)
                            return await GoBackEditServer(server,
                                "Could not install service or server!: \n*******************************************Start*************\n" +
                                result, true);
                    }
                    catch (CommandExceptionCreateServerDuplicate e)
                    {
                        return await GoBackEditServer(server, "Duplicate server entry exception: " + e.Message);
                    }

                try
                {
                    //save and validate server a last time
                    resultServer = await _pavlovServerService.Upsert(server.toPavlovServer(server));
                }
                catch (SaveServerException e)
                {
                    return await GoBackEditServer(server,
                        "Could not validate server after fully setting it up: " + e.Message, server.create);
                }
                catch (CommandException e)
                {
                    return await GoBackEditServer(server,
                        "Could not validate server after fully setting it up: " + e.Message, server.create);
                }
            }
            catch (SaveServerException e)
            {
                if (e.FieldName == "" && e.Message.ToLower().Contains("telnet"))
                    ModelState.AddModelError("TelnetPassword", e.Message);
                else if (e.FieldName == "")
                    ModelState.AddModelError("Id", e.Message);
                else
                    ModelState.AddModelError(e.FieldName, e.Message);
            }

            if (ModelState.ErrorCount > 0) return await EditServer(server);
            if (server.create)
                return Redirect("/PavlovServer/EditServerSelectedMaps/"+resultServer.Id);
                
            return RedirectToAction("Index", "SshServer");
        }

        private async Task<IActionResult> GoBackEditServer(PavlovServerViewModel server, string error,
            bool remove = false)
        {
            if (remove)
                await _service.RemovePavlovServerFromDisk(server);
            ModelState.AddModelError("Id", error
            );
            return await EditServer(server);
        }


        [HttpGet("[controller]/DeleteServer/{id}")]
        public async Task<IActionResult> DeleteServer(int id)
        {
            try
            {
                await _pavlovServerService.Delete(id);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }

        [HttpGet("[controller]/CompleteRemoveView/{id}")]
        public async Task<IActionResult> CompleteRemoveView(int id)
        {
            var server = await _pavlovServerService.FindOne(id);
            if (server == null) return BadRequest("There is no Server with this id!");


            return await EditServer(server.Id, server.SshServer.Id, false, true);
        }

        [HttpPost("[controller]/CompleteRemove/")]
        public async Task<IActionResult> CompleteRemove(PavlovServerViewModel viewModel)
        {
            try
            {
                var result = await _service.RemovePavlovServerFromDisk(viewModel);
                if (result.Value != null) return BadRequest(result.Value);

                await _pavlovServerService.Delete(viewModel.Id);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }

        [HttpPost("[controller]/DeleteServerFromFolder/")]
        public async Task<IActionResult> DeleteServerFromFolder(PavlovServerViewModel viewModel)
        {
            try
            {
                await _service.RemovePavlovServerFromDisk(viewModel);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }

        [HttpGet("[controller]/EditServerSettings/{serverId}")]
        public async Task<IActionResult> EditServerSettings(int serverId)
        {
            var viewModel = new PavlovServerGameIni();
            var server = await _pavlovServerService.FindOne(serverId);
            try
            {
                viewModel.ReadFromFile(server,_notifyService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            viewModel.serverId = serverId;
            return View("ServerSettings", viewModel);
        }

        [HttpGet("[controller]/StartSystemdService/{serverId}")]
        public async Task<IActionResult> StartSystemdService(int serverId)
        {

            var server = await _pavlovServerService.FindOne(serverId);
            try
            {
                await RconStatic.SystemDStart(server,_pavlovServerService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }

        [HttpGet("[controller]/UpdatePavlovServer/{serverId}")]
        public async Task<IActionResult> UpdatePavlovServer(int serverId)
        {

            var server = await _pavlovServerService.FindOne(serverId);

            var result = "";
            try
            {
                result = await RconStatic.UpdateInstallPavlovServer(server,_pavlovServerService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return new ObjectResult(result);
        }

        [HttpGet("[controller]/StopSystemdService/{serverId}")]
        public async Task<IActionResult> StopSystemdService(int serverId)
        {

            var server = await _pavlovServerService.FindOne(serverId);
            try
            {
                await RconStatic.SystemDStop(server,_pavlovServerService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }

        [HttpPost("[controller]/SaveServerSettings/")]
        public async Task<IActionResult> SaveServerSettings(PavlovServerGameIni pavlovServerGameIni)
        {

            var server = await _pavlovServerService.FindOne(pavlovServerGameIni.serverId);
            var selectedMaps = await _serverSelectedMapService.FindAllFrom(server);
            try
            {
                pavlovServerGameIni.SaveToFile(server, selectedMaps,_notifyService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            return RedirectToAction("Index", "SshServer");
        }


        [HttpGet("[controller]/EditWhiteList/{serverId}")]
        public async Task<IActionResult> EditWhiteList(int serverId)
        {

            var server = await _pavlovServerService.FindOne(serverId);
            var steamIds = (await _steamIdentityService.FindAll()).ToArray();
            var selectedSteamIds = (await _whitelistService.FindAllFrom(server)).ToArray();
            //service
            var model = new PavlovServerWhitelistViewModel
            {
                steamIds = selectedSteamIds.Select(x => x.SteamIdentityId).ToList(),
                pavlovServerId = server.Id
            };

            ViewBag.SteamIdentities = steamIds.Select(x => x.Id).ToList();
            return View("WhiteList", model);
        }

        [HttpPost("[controller]/SaveWhiteList/")]
        public async Task<IActionResult> SaveWhiteList(PavlovServerWhitelistViewModel pavlovServerWhitelistViewModel)
        {
            var server = await _pavlovServerService.FindOne(pavlovServerWhitelistViewModel.pavlovServerId);
            try
            {
                await _whitelistService.SaveWhiteListToFileAndDb(pavlovServerWhitelistViewModel.steamIds, server);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            //service
            return RedirectToAction("Index", "SshServer");
        }


        [HttpGet("[controller]/EditModList/{serverId}")]
        public async Task<IActionResult> EditModList(int serverId)
        {

            var server = await _pavlovServerService.FindOne(serverId);
            var tmpUserIds = (await _userservice.FindAll());
            var userIds = new List<LiteDbUser>();
            var isAdmin = false;
            var isMod = false;

            foreach (var user in tmpUserIds)
                if (user != null)
                {
                    isAdmin = await UserManager.IsInRoleAsync(user, "Admin");
                    isMod = await UserManager.IsInRoleAsync(user, "Mod");
                    var steamIdentity = await _steamIdentityService.FindOne(user.Id);
                    if (!isAdmin && !isMod && steamIdentity!=null) userIds.Add(user);
                }

            var selectedUserIds = (await _serverSelectedModsService.FindAllFrom(server));
            //service
            var model = new PavlovServerModlistViewModel
            {
                userIds = selectedUserIds.Select(x => x.LiteDbUser.Id.ToString()).ToList(),
                pavlovServerId = server.Id
            };

            ViewBag.Users = userIds.ToList();
            return View("ModList", model);
        }

        [HttpPost("[controller]/SaveModList/")]
        public async Task<IActionResult> SaveModList(PavlovServerModlistViewModel pavlovServerModlistViewModel)
        {
            var server = await _pavlovServerService.FindOne(pavlovServerModlistViewModel.pavlovServerId);
            try
            {
                await _serverSelectedModsService.SaveWhiteListToFileAndDb(pavlovServerModlistViewModel.userIds, server);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            //service
            return RedirectToAction("Index", "SshServer");
        }
    }
}