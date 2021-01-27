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
using PavlovRconWebserver.Models.ManageViewModels;
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
        private readonly ServerBansService _serverBansService;
        
        public RconController(RconService service,
            SshServerSerivce serverService,
            UserService userService,
            ServerSelectedMapService serverSelectedMapService,
            MapsService mapsService,
            PavlovServerService pavlovServerService,
            ServerBansService serverBansService)
        {
            _service = service;
            _serverService = serverService;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
            _serverBansService = serverBansService;
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

        [HttpPost("[controller]/GetBansFromServers")]
        public async Task<IActionResult> GetBansFromServers(int serverId)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            if (serverId<=0) return BadRequest("Please choose a server!");
            var server = await _pavlovServerService.FindOne(serverId);
            var banlist = await _serverBansService.FindAllFromPavlovServerId(serverId,true);

            return PartialView("/Views/Rcon/BanList.cshtml", banlist);
        }
        
        [HttpPost("[controller]/AddBanPlayer")]
        public async Task<IActionResult> AddBanPlayer(int serverId,string steamId,string timespan)
        {
            if (serverId<=0) return BadRequest("Please choose a server!");
            if (string.IsNullOrEmpty(steamId) || steamId == "-") return BadRequest("SteamId must be set!");
            if (string.IsNullOrEmpty(timespan)) return BadRequest("TimeSpan  must be set!");
            var ban = new ServerBans();
            ban.SteamId = steamId;
            var convert = Statics.BanList.TryGetValue(timespan,out var timespans);
            if (!convert) return BadRequest("Could not convert the timespan!");
            ban.BanSpan = timespans;
            ban.BannedDateTime = DateTime.Now;
            ban.PavlovServer = await _pavlovServerService.FindOne(serverId);
            
            //Get steam name
            try
            {
                var playersTmp = await _service.SendCommand(ban.PavlovServer, "RefreshList");
                var playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
                var playerName = playersList.PlayerList.FirstOrDefault(x => x.UniqueId == steamId)?.Username;
                if (playerName != null)
                {
                    ban.SteamName = playerName; 
                }
            }
            catch (CommandException e)
            {
                //Ignore cause the player name is not something very important
            }
            
            try
            {
                await _service.SendCommand(ban.PavlovServer, "Ban "+steamId);
            }
            catch (CommandException e)
            {
                return BadRequest("Could not ban player on the PavlovServer!");
            }
            var banlist = await _serverBansService.FindAllFromPavlovServerId(ban.PavlovServer.Id,true);
            banlist = await _service.GetServerBansFromBlackList(ban.PavlovServer, banlist);
            //needs to handle the Blacklist file. Also save the file here with all current banned players
            if (banlist.FirstOrDefault(x => x.SteamId == ban.SteamId) == null)
            {
                //write to BlackList.txt
                banlist.Add(ban);
                await _service.SaveBlackListEntry(ban.PavlovServer,banlist);
                
            }
            
            await _serverBansService.Upsert(ban);

            
            return new ObjectResult(true);
        }
        
        [HttpPost("[controller]/RemoveBanPlayer")]
        public async Task<IActionResult> RemoveBanPlayer(int serverId,string steamId )
        {
            if (serverId<=0) return BadRequest("Please choose a server!");
            if (string.IsNullOrEmpty(steamId) || steamId == "-") return BadRequest("SteamID must be set!");
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            

            //Remove from blacklist file
            var banlist = await _service.GetServerBansFromBlackList(pavlovServer, new List<ServerBans>());
            var toRemove = banlist.FirstOrDefault(x => x.SteamId == steamId);
            if (toRemove != null)
            {
                //write to BlackList.txt
                banlist.Remove(toRemove);
                await _service.SaveBlackListEntry(pavlovServer,banlist);
                
            }
            // remove from Database
            var actualBans = await _serverBansService.FindAllFromPavlovServerId(serverId,true);
            var toRemoveBan = actualBans.FirstOrDefault(x => x.SteamId == steamId);
            if (toRemoveBan != null)
            {
                await _serverBansService.Delete(toRemoveBan.Id);
            }
            
            Task.Delay(500).Wait();
            //unban command
            try
            {
                await _service.SendCommand(pavlovServer, "Unban "+steamId);
            }
            catch (CommandException e)
            {
                return BadRequest("Could not unban player from in memory blacklist!");
            }
            return new ObjectResult(true);
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
        
        
        [HttpPost("[controller]/GetTeamList")]
        public async Task<IActionResult> GetTeamList(int serverId)
        {
            
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();
            if (serverId<=0) return BadRequest("Please choose a server!");
            var server = await _pavlovServerService.FindOne(serverId);
            var playersTmp = "";
            var extendetList = new List<PlayerModelExtended>();
            PlayerListClass playersList = new PlayerListClass();
            try
            {
                playersTmp = await _service.SendCommand(server, "RefreshList");
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }
            playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
            var serverInfo = "";
            try
            {
                serverInfo = await _service.SendCommand(server, "ServerInfo");
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(serverInfo);
            
            foreach (var player in playersList.PlayerList)
            {
                var playerInfo = await _service.SendCommand(server, "InspectPlayer " + player.UniqueId);
                var singlePlayer = JsonConvert.DeserializeObject<PlayerModelExtendedRconModel>(playerInfo);
                singlePlayer.PlayerInfo.Username = player.Username;
                extendetList.Add(singlePlayer.PlayerInfo);
            }
            ViewBag.team0Score = tmp.ServerInfo.Team0Score;
            ViewBag.team1Score = tmp.ServerInfo.Team1Score;
            return PartialView("/Views/Rcon/PlayerList.cshtml",extendetList);
        }

        

    }

}