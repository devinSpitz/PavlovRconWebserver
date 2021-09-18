using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    public class RconController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly ServerBansService _serverBansService;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly RconService _service;
        private readonly UserService _userservice;

        public RconController(RconService service,
            UserService userService,
            ServerSelectedMapService serverSelectedMapService,
            MapsService mapsService,
            PavlovServerService pavlovServerService,
            ServerBansService serverBansService,
            ServerSelectedModsService serverSelectedModsService,
            PavlovServerPlayerService pavlovServerPlayerService)
        {
            _service = service;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
            _serverBansService = serverBansService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _serverSelectedModsService = serverSelectedModsService;
        }


        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new RconViewModel();
            var servers = (await _pavlovServerService.FindAll())
                .Where(x => x.ServerServiceState == ServerServiceState.active)
                .ToList(); // has to be the same as in await IsModSomeWhere(); 
            var isModSomeWhere = false;
            var user = await _userservice.getUserFromCp(HttpContext.User);
            isModSomeWhere = await _pavlovServerService.IsModSomeWhere(user, _serverSelectedModsService);

            if (isModSomeWhere)
            {
                // cut off all server he is not mod
                var tmpServers = new List<PavlovServer>();
                foreach (var pavlovServer in servers)
                    if (await RightsHandler.IsModOnTheServer(_serverSelectedModsService, pavlovServer, user.Id))
                        tmpServers.Add(pavlovServer);

                servers = tmpServers;
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice))
                    return Unauthorized();
            }

            ViewBag.Servers = servers;
            //set allowed Commands
            var allowCommands = new List<string>();
            ViewBag.commandsAllow =
                await RightsHandler.GetAllowCommands(viewModel, HttpContext.User, _userservice, isModSomeWhere);

            return View(viewModel);
        }


        [HttpPost("[controller]/SendCommand")]
        public async Task<IActionResult> SendCommand(int server, string command)
        {
            var singleServer = new PavlovServer();
            singleServer = await _pavlovServerService.FindOne(server);
            var isModOnTheServer = await IsModOnTheServer(server);
            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice) &&
                !isModOnTheServer) return Unauthorized();
            if (!await RightsHandler.IsUserAtLeastInRoleForCommand(command, HttpContext.User, _userservice,
                isModOnTheServer)) return Unauthorized();

            var response = "";
            try
            {
                response = await RconStatic.SendCommandSShTunnel(singleServer, command);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            if (string.IsNullOrEmpty(response)) return BadRequest("The response was empty!");
            return new ObjectResult(response);
        }

        [HttpPost("[controller]/SingleServerInfoPartialView")]
        public async Task<IActionResult> SingleServerInfoPartialView(string server, int serverId)
        {
            var isModOnTheServer = await IsModOnTheServer(serverId);
            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice) &&
                !isModOnTheServer) return Unauthorized();
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server.Replace("\"\"", "\"ServerInfo\""));
            if (tmp == null) return BadRequest("Could not Desirialize Object!");
            var pavlovServer = await _pavlovServerService.FindOne(serverId);
            if (pavlovServer == null) return BadRequest("Could not get pavlovserver!");
            tmp.Name = pavlovServer.Name;

            var map = await _mapsService.FindOne(tmp.ServerInfo.MapLabel.Replace("UGC", ""));
            if (map != null)
                tmp.ServerInfo.MapPictureLink = map.ImageUrl;
            return PartialView("PavlovServerInfoPartialView", tmp);
        }

        [HttpPost("[controller]/RconChooseItemPartialView")]
        public async Task<IActionResult> RconChooseItemPartialView()
        {
            return PartialView("PavlovChooseItemPartialView");
        }

        [HttpPost("[controller]/PavlovChooseMapPartialView")]
        public async Task<IActionResult> PavlovChooseMapPartialView(int? serverId)
        {
            //onMutliRcon do not handle the selected maps
            List<Map> listOfMaps;

            listOfMaps = (await _mapsService.FindAll()).ToList();
            if (serverId != null)
            {
                var server = await _pavlovServerService.FindOne((int) serverId);
                var mapsSelected = await _serverSelectedMapService.FindAllFrom(server);
                if (mapsSelected != null)
                {
                    foreach (var map in listOfMaps)
                        if (mapsSelected.FirstOrDefault(x => x.Map.Id == map.Id) != null)
                            map.sort = 1;
                    listOfMaps = listOfMaps.OrderByDescending(x => x.sort).ToList();
                }
            }

            return PartialView("~/Views/Rcon/PavlovChooseMapPartialView.cshtml", listOfMaps);
        }

        [HttpPost("[controller]/GetBansFromServers")]
        public async Task<IActionResult> GetBansFromServers(int serverId)
        {
            var isModOnTheServer = await IsModOnTheServer(serverId);
            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice) &&
                !isModOnTheServer) return Unauthorized();
            if (serverId <= 0) return BadRequest("Please choose a server!");
            var server = await _pavlovServerService.FindOne(serverId);
            var banlist = await _serverBansService.FindAllFromPavlovServerId(serverId, true);

            ViewBag.ServerId = serverId;
            return PartialView("/Views/Rcon/BanList.cshtml", banlist);
        }

        [HttpPost("[controller]/AddBanPlayer")]
        public async Task<IActionResult> AddBanPlayer(int serverId, string steamId, string timespan)
        {
            var isModOnTheServer = await IsModOnTheServer(serverId);
            if (!await RightsHandler.IsUserAtLeastInRole("Mod", HttpContext.User, _userservice) && !isModOnTheServer)
                return Unauthorized();
            if (serverId <= 0) return BadRequest("Please choose a server!");
            if (string.IsNullOrEmpty(steamId) || steamId == "-") return BadRequest("SteamId must be set!");
            if (string.IsNullOrEmpty(timespan)) return BadRequest("TimeSpan  must be set!");
            var ban = new ServerBans();
            ban.SteamId = steamId;
            var convert = Statics.BanList.TryGetValue(timespan, out var timespans);
            if (!convert) return BadRequest("Could not convert the timespan!");
            ban.BanSpan = timespans;
            ban.BannedDateTime = DateTime.Now;
            ban.PavlovServer = await _pavlovServerService.FindOne(serverId);

            //Get steam name
            try
            {
                var result1 = "";
                try
                {
                    result1 = await RconStatic.SendCommandSShTunnel(ban.PavlovServer, "RefreshList");
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }

                var playersTmp = result1;
                var playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
                var playerName = playersList.PlayerList.FirstOrDefault(x => x.UniqueId == steamId)?.Username;
                if (playerName != null) ban.SteamName = playerName;
            }
            catch (CommandException)
            {
                //Ignore cause the player name is not something very important
            }

            var result = "";
            try
            {
                result = await RconStatic.SendCommandSShTunnel(ban.PavlovServer, "Ban " + steamId);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            var banlist = await _serverBansService.FindAllFromPavlovServerId(ban.PavlovServer.Id, true);
            try
            {
                banlist = await _service.GetServerBansFromBlackList(ban.PavlovServer, banlist);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            //needs to handle the Blacklist file. Also save the file here with all current banned players
            if (banlist.FirstOrDefault(x => x.SteamId == ban.SteamId) == null)
            {
                //write to BlackList.txt
                banlist.Add(ban);
                try
                {
                    await _service.SaveBlackListEntry(ban.PavlovServer, banlist);
                }
                catch (CommandException e)
                {
                    return BadRequest(e.Message);
                }
            }

            await _serverBansService.Upsert(ban);


            return new ObjectResult(true);
        }

        [HttpPost("[controller]/RemoveBanPlayer")]
        public async Task<IActionResult> RemoveBanPlayer(int serverId, string steamId)
        {
            if (!await RightsHandler.IsUserAtLeastInRole("Mod", HttpContext.User, _userservice)) return Unauthorized();
            if (serverId <= 0) return BadRequest("Please choose a server!");
            if (string.IsNullOrEmpty(steamId) || steamId == "-") return BadRequest("SteamID must be set!");
            var pavlovServer = await _pavlovServerService.FindOne(serverId);

            var banlist = new List<ServerBans>();
            //Remove from blacklist file
            try
            {
                banlist = await _service.GetServerBansFromBlackList(pavlovServer, new List<ServerBans>());
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            var toRemove = banlist.FirstOrDefault(x => x.SteamId == steamId);
            if (toRemove != null)
            {
                //write to BlackList.txt
                banlist.Remove(toRemove);
                try
                {
                    await _service.SaveBlackListEntry(pavlovServer, banlist);
                }
                catch (CommandException e)
                {
                    return BadRequest(e.Message);
                }
            }

            // remove from Database
            var actualBans = await _serverBansService.FindAllFromPavlovServerId(serverId, true);
            var toRemoveBan = actualBans.FirstOrDefault(x => x.SteamId == steamId);
            if (toRemoveBan != null) await _serverBansService.Delete(toRemoveBan.Id);

            Task.Delay(1000).Wait(); // If you not wait it may just don't work. Don't know why
            //unban command
            try
            {
                await RconStatic.SendCommandSShTunnel(pavlovServer, "Unban " + steamId);
            }
            catch (CommandException)
            {
                return BadRequest("Could not unban player from in memory blacklist!");
            }

            return new ObjectResult(true);
        }


        [HttpPost("[controller]/JsonToHtmlPartialView")]
        public async Task<IActionResult> JsonToHtmlPartialView(string json)
        {
            return PartialView("/Views/JsonToHtmlPartialView.cshtml", json);
        }

        [HttpPost("[controller]/ValueFieldPartialView")]
        public async Task<IActionResult> ValueFieldPartialView(List<Command> playerCommands,
            List<ExtendedCommand> twoValueCommands, string atualCommandName, bool isNormalCommand, bool firstValue)
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

        [HttpPost("[controller]/GetAllPlayers")]
        public async Task<IActionResult> GetAllPlayers(int serverId)
        {
            var isModOnTheServer = await IsModOnTheServer(serverId);
            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice) &&
                !isModOnTheServer) return Unauthorized();
            if (serverId <= 0) return BadRequest("Please choose a server!");
            var playersList = new PlayerListClass();


            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            playersList.PlayerList = players.Select(x => new PlayerModel
            {
                Username = x.Username,
                UniqueId = x.UniqueId
            }).ToList();
            return Ok(playersList);
        }

        private async Task<bool> IsModOnTheServer(int serverId)
        {
            var singleServer = new PavlovServer();
            singleServer = await _pavlovServerService.FindOne(serverId);
            if (HttpContext.User == null) return false;
            var user = await _userservice.getUserFromCp(HttpContext.User);
            if (user == null) return false;
            if (singleServer == null) return false;
            if (_serverSelectedModsService == null) return false;
            var isModOnTheServer = await RightsHandler.IsModOnTheServer(_serverSelectedModsService, singleServer,
                user.Id);
            return isModOnTheServer;
        }


        [HttpPost("[controller]/GetTeamList")]
        public async Task<IActionResult> GetTeamList(int serverId)
        {
            var isModOnTheServer = await IsModOnTheServer(serverId);
            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userservice) &&
                !isModOnTheServer) return Unauthorized();
            if (serverId <= 0) return BadRequest("Please choose a server!");

            var server = await _pavlovServerService.FindOne(serverId);
            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            var serverInfo = "";
            try
            {
                var result = "";
                try
                {
                    result = await RconStatic.SendCommandSShTunnel(server, "ServerInfo");
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }

                serverInfo = result;
            }
            catch (CommandException e)
            {
            }

            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(serverInfo.Replace("\"\"", "\"ServerInfo\""));
            var model = new PavlovServerPlayerListViewModel
            {
                PlayerList = players.Select(x => new PlayerModelExtended
                {
                    Cash = x.Cash,
                    KDA = x.KDA,
                    Score = x.Score,
                    TeamId = x.TeamId,
                    UniqueId = x.UniqueId,
                    Username = x.Username
                }).ToList(),
                team0Score = tmp.ServerInfo.Team0Score,
                team1Score = tmp.ServerInfo.Team1Score
            };


            return PartialView("/Views/Rcon/PlayerList.cshtml", model);
        }
    }
}