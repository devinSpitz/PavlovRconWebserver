using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PavlovRconWebserver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{   
    [Authorize]
    public class RconController : Controller
    {
        private readonly RconService _service;
        private readonly RconServerSerivce _serverService;
        public RconController(RconService service,RconServerSerivce serverService)
        {
            _service = service;
            _serverService = serverService;
        }
        public IActionResult Index(RconViewModel viewModel = null)
        {
            ViewBag.Servers = _serverService.FindAll();
            return View(viewModel);
        }

        public async Task<List<string>> sendCommand(List<int> servers, string command)
        {
            var answers = new List<string>();
            foreach (var server in servers)
            {
                var rconServer = _serverService.FindOne(server);
                var response = await _service.SendCommand(rconServer, command);
                if (response != null)
                {
                    answers.Add(response); 
                }
                else
                {
                    throw new Exception("Could not connect or login to: "+rconServer.Adress); 
                }
            }

            return answers;
        }
        
        public IActionResult RconServerInfoPartialView(string[] servers,List<int> serverIds)
        {
            List<ServerInfoViewModel> list = new List<ServerInfoViewModel>();
            var count = 0;
            foreach (var server in servers)
            {
                var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
                tmp.Adress = _serverService.FindOne(serverIds[count]).Adress;
                list.Add(tmp);
                count++;
            }
            return PartialView("RconServerInfoPartialView", list);
        }
        
        public IActionResult JsonToHtmlPartialView(string json)
        {
            return PartialView("/Views/JsonToHtmlPartialView.cshtml", json);
        }
        
        public IActionResult ValueFieldPartialView(List<Command> playerCommands,List<ExtendedCommand> twoValueCommands,string atualCommandName,bool isNormalCommand,bool firstValue)
        {
            
            return PartialView("/Views/Rcon/ValueFieldPartialView.cshtml", new ValueFieldPartialViewViewModel
            {
                PlayerCommands = playerCommands,
                TwoValueCommands = twoValueCommands,
                ActualCommandName = atualCommandName,
                IsNormalCommand = isNormalCommand,
                firstValue = firstValue
            });
        }
        
        public async Task<PlayerListClass> GetAllPlayers(int serverId)
        {
            
            PlayerListClass playersList = new PlayerListClass();
            var server = _serverService.FindOne(serverId);
            var playersTmp = await _service.SendCommand(server, "RefreshList");
            playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
            return playersList;
        }
        
        

    }

}