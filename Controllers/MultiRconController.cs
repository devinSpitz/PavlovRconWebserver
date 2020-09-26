using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PavlovRconWebserver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{   
    [Authorize]
    public class MultiRconController : Controller
    {
        private readonly RconService _service;
        private readonly SshServerSerivce _serverService;
        private readonly UserService _userservice;
        private readonly PavlovServerService _pavlovServerService;
        
        public MultiRconController(RconService service,SshServerSerivce serverService,UserService userService,PavlovServerService pavlovServerService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _pavlovServerService = pavlovServerService;
        }

        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            if(!await  RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return new UnauthorizedResult();
            RconViewModel viewModel = new RconViewModel();
            viewModel.MultiRcon = true;
            ViewBag.Servers = await _pavlovServerService.FindAll();
            ViewBag.commandsAllow = await RightsHandler.GetAllowCommands(viewModel, HttpContext.User, _userservice);
            return View("/Views/Rcon/Index.cshtml",viewModel);
        }

        [HttpPost("[controller]/sendCommand/")]
        public async Task<IActionResult> SendCommand(int[] servers, string command)
        {
            
            var results = new List<string>();
            
            if(!await  RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            if(!await RightsHandler.IsUserAtLeastInRoleForCommand(command, HttpContext.User, _userservice)) return Unauthorized();
            
            foreach (var server in servers)
            {
                var response = "";
                var singleServer = new PavlovServer();
                try
                {
                    singleServer = await _pavlovServerService.FindOne(server);
                    response = await _service.SendCommand(singleServer, command);
                }
                catch (CommandException e)
                {
                    results.Add(e.Message);
                    continue;
                }

                if (String.IsNullOrEmpty(response))
                {
                    results.Add("The response from "+singleServer.SshServer.Adress+" was empty!");
                    continue;
                }

                if (command != "ServerInfo")
                {
                    response = "\""+singleServer.SshServer.Name +"\": "+ response;
                }
                results.Add(response);
            };
            if (command != "ServerInfo")
            {
                return new ObjectResult("{" + String.Join(",", results) + "}");
            }
            else
            {
                return new ObjectResult(results);
            }
        }
        
        [HttpPost("[controller]/SingleServerInfoPartialView")]
        public async Task<IActionResult> SingleServerInfoPartialView(string[] servers,int[] serverIds)
        {
            if(!await  RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return new UnauthorizedResult();
            var count = 0;
            var serverList = new List<ServerInfoViewModel>();
            foreach (var server in servers)
            {
                var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
                var sshServer = await _serverService.FindOne(serverIds[count]);
                tmp.Name = sshServer.Name;
                serverList.Add(tmp);
                
                count++;
            }
            return PartialView("/Views/Rcon/SshServerInfoMultiPartialView.cshtml", serverList);
        }

    }

}