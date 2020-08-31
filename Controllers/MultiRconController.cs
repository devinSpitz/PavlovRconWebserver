using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PavlovRconWebserver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{   
    [Authorize]
    public class MultiRconController : RconController
    {
        private readonly RconService _service;
        private readonly RconServerSerivce _serverService;
        private readonly UserService _userservice;
        
        public MultiRconController(RconService service,RconServerSerivce serverService,UserService userService) : base(service,serverService,userService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
        }

        [Route("[controller]/")]
        public async Task<IActionResult> Index(RconViewModel viewModel = null)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            viewModel.MultiRcon = true;
            ViewBag.Servers = _serverService.FindAll();
            return View("/Views/Rcon/Index.cshtml",viewModel);
        }


        [Route("[controller]/sendCommand")]
        public async Task<IActionResult> SendCommand(int[] servers, string command)
        {
            var results = new List<string>();
            if(!await CheckRights())  return Unauthorized();
            foreach (var server in servers)
            {
                var response = "";
                var rconServer = new RconServer();
                try
                {
                    rconServer = _serverService.FindOne(server);
                    response = await _service.SendCommand(rconServer, command);
                }
                catch (CommandException e)
                {
                    results.Add(e.Message);
                    continue;
                }

                if (String.IsNullOrEmpty(response))
                {
                    results.Add("The response from "+rconServer.Adress+" was empty!");
                    continue;
                }

                if (command != "ServerInfo")
                {
                    response = "\""+rconServer.Name +"\": "+ response;
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
        
        [Route("[controller]/RconServerInfoPartialView")]
        public async Task<IActionResult> RconServerInfoPartialView(string[] servers,int[] serverIds)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            var count = 0;
            var serverList = new List<ServerInfoViewModel>();
            foreach (var server in servers)
            {
                var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
                var rconServer = _serverService.FindOne(serverIds[count]);
                tmp.Name = rconServer.Name;
                serverList.Add(tmp);
                
                count++;
            }
            return PartialView("/Views/Rcon/RconServerInfoMultiPartialView.cshtml", serverList);
        }

    }

}