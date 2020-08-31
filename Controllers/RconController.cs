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
        private readonly UserService _userservice;
        public RconController(RconService service,RconServerSerivce serverService,UserService userService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
        }
        public async Task<IActionResult> Index(RconViewModel viewModel = null)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            viewModel.MultiRcon = false;
            ViewBag.Servers = _serverService.FindAll();
            return View(viewModel);
        }

        public async Task<bool> CheckRights()
        {
            return await _userservice.IsUserInRole("Admin", HttpContext.User) || await _userservice.IsUserInRole("User", HttpContext.User);
        }

        public async Task<IActionResult> SendCommand(int server, string command)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
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
        
        public async Task<IActionResult> RconServerInfoPartialView(string server,int serverId)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
            tmp.Name = _serverService.FindOne(serverId).Name;
            return PartialView("RconServerInfoPartialView", tmp);
        }
        
        public async Task<IActionResult> RconChooseItemPartialView()
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            return PartialView("RconChooseItemPartialView");
        }
        public async Task<IActionResult> RconChooseMapPartialView()
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            var listOfMaps = await _service.CrawlSteamMaps();
            return PartialView("~/Views/Rcon/RconChooseMapPartialView.cshtml",listOfMaps);
        }
        
        public async Task<IActionResult> JsonToHtmlPartialView(string json)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
            return PartialView("/Views/JsonToHtmlPartialView.cshtml", json);
        }
        
        public async Task<IActionResult> ValueFieldPartialView(List<Command> playerCommands,List<ExtendedCommand> twoValueCommands,string atualCommandName,bool isNormalCommand,bool firstValue)
        {
            if(!await CheckRights())  return new UnauthorizedResult();
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
            if(!await CheckRights())  return new UnauthorizedResult();
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