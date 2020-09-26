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
        private readonly SshServerSerivce _serverService;
        private readonly UserService _userservice;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly MapsService _mapsService;
        private readonly PavlovServerService _pavlovServerService;
        
        public RconController(RconService service,SshServerSerivce serverService,UserService userService,ServerSelectedMapService serverSelectedMapService,MapsService mapsService,PavlovServerService pavlovServerService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
        }
        
   
        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            var viewModel = new RconViewModel();
            viewModel.MultiRcon = false;
            ViewBag.Servers = await _pavlovServerService.FindAll();
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
            var singleServer = new PavlovServer();
            try
            {
                singleServer = await _pavlovServerService.FindOne(server);
                response = await _service.SendCommand(singleServer, command);
            }
            catch (CommandException e)
            {
               return BadRequest(e.Message);
            }

            if (String.IsNullOrEmpty(response)) return BadRequest("The response was empty!");
            return new ObjectResult(response);
        }
        
        [HttpPost("[controller]/SingleServerInfoPartialView")]
        public async Task<IActionResult> SingleServerInfoPartialView(string server,int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server);
            tmp.Name = (await _pavlovServerService.FindOne(serverId)).Name;
            return PartialView("PavlovServerInfoPartialView", tmp);
        }
        
        [HttpPost("[controller]/RconChooseItemPartialView")]
        public async Task<IActionResult> RconChooseItemPartialView()
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            return PartialView("PavlovChooseItemPartialView");
        }
        
        [HttpPost("[controller]/PavlovChooseMapPartialView")]
        public async Task<IActionResult> PavlovChooseMapPartialView(int? serverId)
        {
            //onMutliRcon do not handle the selected maps
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            List<Map> listOfMaps;
            
            listOfMaps = (await _mapsService.FindAll()).ToList();
            if (serverId != null)
            {
                var server = await _serverService.FindOne((int)serverId);
                var mapsSelected = await  _serverSelectedMapService.FindAllFrom(server);
                if (mapsSelected != null)
                {
               
                    foreach (var map in listOfMaps)
                    {
                        if (mapsSelected.FirstOrDefault(x => x.Map.Id == map.Id) != null)
                        {
                            map.sort = 1;
                        }
                    } 
                    listOfMaps = listOfMaps.OrderByDescending(x=>x.sort).ToList();
                }
                
            }
            return PartialView("~/Views/Rcon/PavlovChooseMapPartialView.cshtml",listOfMaps);
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
            var server = await _pavlovServerService.FindOne(serverId);
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