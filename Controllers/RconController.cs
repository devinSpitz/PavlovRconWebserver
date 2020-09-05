using System;
using System.Collections.Generic;
using System.Linq;
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
    public class RconController : Controller
    {
        private readonly RconService _service;
        private readonly RconServerSerivce _serverService;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        public RconController(RconService service,RconServerSerivce serverService,UserService userService,ServerSelectedMapService serverSelectedMapService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
        }
        
   
        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            var viewModel = new RconViewModel();
            viewModel.MultiRcon = false;
            ViewBag.Servers = _serverService.FindAll();
            //set allowed Commands
            List<string> allowCommands = new List<string>();
            ViewBag.commandsAllow = await RightsHandler.GetAllowCommands(viewModel, HttpContext.User, _userservice);
            
            return View(viewModel);
        }


        [HttpPost("[controller]/SendCommand")]
        public async Task<IActionResult> SendCommand(int server, string command)
        {
           if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            if(!await RightsHandler.IsUserAtLeastInRoleForCommand(command, HttpContext.User, _userservice)) return Unauthorized();
            
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
        
        [HttpPost("[controller]/RconServerInfoPartialView")]
        public async Task<IActionResult> RconServerInfoPartialView(string server,int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
            tmp.Name = _serverService.FindOne(serverId).Name;
            return PartialView("RconServerInfoPartialView", tmp);
        }
        
        [HttpPost("[controller]/RconChooseItemPartialView")]
        public async Task<IActionResult> RconChooseItemPartialView()
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            return PartialView("RconChooseItemPartialView");
        }
        
        [HttpPost("[controller]/RconChooseMapPartialView")]
        public async Task<IActionResult> RconChooseMapPartialView(int? serverId)
        {
            //onMutliRcon do not handle the selected maps
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            List<RconMapViewModel> listOfMaps;
            if (serverId != null)
            {
                var server = _serverService.FindOne((int)serverId);
                listOfMaps = await Steam.CrawlSteamMaps(_serverSelectedMapService.FindAllFrom(server).ToList());
            }
            else
            {
                listOfMaps = await Steam.CrawlSteamMaps();
            }
            return PartialView("~/Views/Rcon/RconChooseMapPartialView.cshtml",listOfMaps);
        }


        
        [HttpPost("[controller]/JsonToHtmlPartialView")]
        public async Task<IActionResult> JsonToHtmlPartialView(string json)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            return PartialView("/Views/JsonToHtmlPartialView.cshtml", json);
        }
        
        [HttpPost("[controller]/ValueFieldPartialView")]
        public async Task<IActionResult> ValueFieldPartialView(List<Command> playerCommands,List<ExtendedCommand> twoValueCommands,string atualCommandName,bool isNormalCommand,bool firstValue)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            return PartialView("/Views/Rcon/ValueFieldPartialView.cshtml", new ValueFieldPartialViewViewModel
            {
                PlayerCommands = playerCommands,
                TwoValueCommands = twoValueCommands,
                ActualCommandName = atualCommandName,
                IsNormalCommand = isNormalCommand,
                firstValue = firstValue
            });
        }
        
        [HttpPost("[controller]/GetAllPlayers")]
        public async Task<IActionResult> GetAllPlayers(int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
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