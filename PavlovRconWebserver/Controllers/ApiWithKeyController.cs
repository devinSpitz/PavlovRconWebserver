﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using Serilog.Events;

namespace PavlovRconWebserver.Controllers
{
    public class ApiWithKeyController : Controller
    {
        public IToastifyService _notifyService { get; }
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly UserService _userService;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly ReservedServersService _reservedServersService;
        private readonly IConfiguration _configuration;
        private readonly string ApiKey;
        private readonly string GeneratedServerPath;
        
        public ApiWithKeyController(PavlovServerService pavlovServerService,
                                    SshServerSerivce sshServerSerivce,
                                    ReservedServersService reservedServersService,
                                    UserService userService,
                                    UserManager<LiteDbUser> userManager,
                                    IToastifyService notyfService,
                                    IConfiguration configuration)
        {
            _notifyService = notyfService;
            _pavlovServerService = pavlovServerService;
            _sshServerSerivce = sshServerSerivce;
            _userService = userService;
            _reservedServersService = reservedServersService;
            _configuration = configuration;
            _userManager = userManager;
            ApiKey = configuration.GetSection("ApiKey").Value;
            GeneratedServerPath = configuration.GetSection("GeneratedServerPath").Value;
        }
        
        [HttpPost("Api/GetListOfAvailable")]
        public async Task<IActionResult> GetListOfAvailable(string apiKey)
        {
            if (!HasAccess(apiKey)) return BadRequest("No Authkey set or wrong auth key!");
            var list = (await _sshServerSerivce.FindAll()).Where(x => x.IsForHosting).ToList();
            return new ObjectResult(list.Select(x => new
            {
                x.Id,
                x.Name
            }).ToList());
        }


        [HttpPost("Api/CreateServer")]
        public async Task<IActionResult> CreateServer(string apiKey,int sshServerId,bool shack,string email)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.IsForHosting) return BadRequest("The ssh server ist not for hosting!");

            var guid = Guid.NewGuid().ToString();
            var model = new PavlovServerViewModel
            {
                Name = "Autogenerated: "+guid,
                TelnetPort = sshServer.PavlovServers.Max(x=>x.TelnetPort)+1,
                DeletAfter = 7,
                TelnetPassword = Guid.NewGuid().ToString(),
                ServerPort = sshServer.PavlovServers.Max(x=>x.ServerPort)+1,
                ServerFolderPath = GeneratedServerPath+guid+"/",
                ServerSystemdServiceName = "pavlov"+guid.Replace("-",""),
                ServerType = ServerType.Community,
                ServerServiceState = ServerServiceState.disabled,
                SshServer = sshServer,
                AutoBalance = false,
                SaveStats = true,
                Shack = shack,
                sshServerId = sshServer.Id,
                create = true,
                SshUsernameRoot = sshServer.SshUsernameRootForHosting,
                SshPasswordRoot = sshServer.SshPasswordRootForHosting,
                SshKeyFileNameRoot = sshServer.SshKeyFileNameRootForHosting,
                SshPassphraseRoot = sshServer.SshPassphraseRootForHosting
            };

            var user = await _userService.GetUserByEmail(email);
            if (user != null)
            {
                model.LiteDbUserId = user.Id.ToString();
                model.Owner = user;
                if(!await _userManager.IsInRoleAsync(user,"ServerRent"))
                    await _userManager.AddToRoleAsync(user, "ServerRent");
            }
            var result = await _pavlovServerService.CreatePavlovServer(model);
            model = result.Key;
            if (result.Value != null)
            {
                await _sshServerSerivce.RemovePavlovServerFromDisk(model);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Could not create rented server! " + model.Name, LogEventLevel.Fatal, _notifyService);
                return BadRequest();
            }
            
            
            var resultServer = await _pavlovServerService.Upsert(model.toPavlovServer(model));
            if (user==null)
            {
                await _reservedServersService.Add(new ReservedServer() { Email = email, ServerId = resultServer.Id});
            }
            return Ok(resultServer.Id);
        }
        [HttpPost("Api/CreateSshServer")]
        public async Task<IActionResult> CreateSshServer(string apiKey,string email)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
           var model = new SshServer
            {
                Name = null,
                SshUsername = "Please Enter",
                SshPassword =  "DoChange"
            };
           var user = await _userService.GetUserByEmail(email);
            if (user != null)
            {
                model.LiteDbUserId = user.Id.ToString();
                model.Owner = user;
                if(!await _userManager.IsInRoleAsync(user,"OnPremise"))
                    await _userManager.AddToRoleAsync(user, "OnPremise");
            }

            var sshServer = await _sshServerSerivce.Insert(model);
            
            if (user==null)
            {
                await _reservedServersService.Add(new ReservedServer() { Email = email, SshServerId = sshServer});
            }
            return Ok(sshServer);
        }
        [HttpPost("Api/StopAndTakeAway")]
        public async Task<IActionResult> StopAndTakeAway(string apiKey,int sshServerId,int pavlovServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            var pavlovServer = await _pavlovServerService.FindOne(pavlovServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            await RconStatic.SystemDStop(pavlovServer,_pavlovServerService);
            pavlovServer.OldOwner = pavlovServer.Owner;
            pavlovServer.Owner = null;
            await _pavlovServerService.Upsert(pavlovServer);
            return Ok();
        }        
        
        [HttpPost("Api/StopAndTakeAwayOnPromise")]
        public async Task<IActionResult> StopAndTakeAwayOnPromise(string apiKey,int sshServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            sshServer.OldOwner = sshServer.Owner;
            sshServer.Owner = null;
            await _sshServerSerivce.Update(sshServer);
            return Ok();
        }        
        
        [HttpPost("Api/RemoveSshServer")]
        public async Task<IActionResult> RemoveSshServer(string apiKey,int sshServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            await _sshServerSerivce.Delete(sshServer.Id);
            return Ok();
        }   
        
        [HttpPost("Api/RemovePavlovServer")]
        public async Task<IActionResult> RemovePavlovServer(string apiKey,int sshServerId,int pavlovServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            var pavlovServer = await _pavlovServerService.FindOne(pavlovServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            var viewmodel = new PavlovServerViewModel();
            viewmodel.fromPavlovServer(pavlovServer,sshServerId);
            //todo add admin
            viewmodel.SshPassphraseRoot = sshServer.SshPassphraseRootForHosting;
            viewmodel.SshPasswordRoot = sshServer.SshPasswordRootForHosting;
            viewmodel.SshUsernameRoot = sshServer.SshUsernameRootForHosting;
            viewmodel.SshKeyFileNameRoot = sshServer.SshKeyFileNameRootForHosting;
            await _sshServerSerivce.RemovePavlovServerFromDisk(viewmodel);
            await _pavlovServerService.Delete(pavlovServer.Id);
            return Ok();
        }        

        
        [HttpPost("Api/GivePavlovServerAgain")]
        public async Task<IActionResult> GivePavlovServerAgain(string apiKey,int sshServerId,int pavlovServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            var pavlovServer = await _pavlovServerService.FindOne(pavlovServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            pavlovServer.Owner = pavlovServer.OldOwner;
            pavlovServer.OldOwner = null;
            await _pavlovServerService.Upsert(pavlovServer);
            return Ok();
        }        
        
        [HttpPost("Api/GiveSshServerAgain")]
        public async Task<IActionResult> GiveSshServerAgain(string apiKey,int sshServerId)
        {
            if (!HasAccess(apiKey)) return BadRequest("No AuthKey set or wrong auth key!");
            var sshServer = await _sshServerSerivce.FindOne(sshServerId);
            if (sshServer == null) return BadRequest("The ssh server does not exist!");
            if (!sshServer.HostingAvailable) return BadRequest("The ssh server ist not for hosting!");
            sshServer.Owner = sshServer.OldOwner;
            sshServer.OldOwner = null;
            await _sshServerSerivce.Update(sshServer);
            return Ok();
        }

        private bool HasAccess(string apiKey)
        {
            if (apiKey != ApiKey || !ApiKeySet(_configuration))
            {
                return false;
            }

            return true;
        }
        
        public static bool ApiKeySet(IConfiguration configurationTmp)
        {
            
            var apiKey = configurationTmp.GetSection("ApiKey").Value;
            if (apiKey == "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" || apiKey.Contains("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX") ||
                apiKey.Length < 64)
            {
                return false;
            }

            return true;
        }
    }
}