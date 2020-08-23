using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PavlovRconWebserver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
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

        public async Task<IActionResult> SendCommand(int server, string command)
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
               return BadRequest(e.Message);
            }

            if (String.IsNullOrEmpty(response)) return BadRequest("The response was empty!");
            return new ObjectResult(response);
        }
        
        public IActionResult RconServerInfoPartialView(string server,int serverId)
        {
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
            tmp.Adress = _serverService.FindOne(serverId).Adress;
            return PartialView("RconServerInfoPartialView", tmp);
        }
        
        public IActionResult RconChooseItemPartialView()
        {
            return PartialView("RconChooseItemPartialView");
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
        
        public async Task<IActionResult> GetAllPlayers(int serverId)
        {
            if (serverId<=0) return BadRequest("Please choose a server!");
            PlayerListClass playersList = new PlayerListClass();
            var server = _serverService.FindOne(serverId);
            var playersTmp = "";
            try
            {
                playersTmp = await _service.SendCommand(server, "RefreshList");
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }
            playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
            return Ok(playersList);
        }
        
        

    }

}