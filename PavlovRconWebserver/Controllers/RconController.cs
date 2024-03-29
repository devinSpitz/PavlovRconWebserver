using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using Serilog.Events;

namespace PavlovRconWebserver.Controllers
{
    [Authorize(Roles = CustomRoles.User)]
    public class RconController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly IToastifyService _toastifyService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerAdminLogsService _pavlovServerAdminLogsService;
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
            PavlovServerAdminLogsService pavlovServerAdminLogsService,
            ServerBansService serverBansService,
            ServerSelectedModsService serverSelectedModsService,
            PavlovServerPlayerService pavlovServerPlayerService,
            IToastifyService itToastifyService)
        {
            _toastifyService = itToastifyService;
            _service = service;
            _userservice = userService;
            _serverSelectedMapService = serverSelectedMapService;
            _mapsService = mapsService;
            _pavlovServerService = pavlovServerService;
            _serverBansService = serverBansService;
            _pavlovServerPlayerService = pavlovServerPlayerService;
            _serverSelectedModsService = serverSelectedModsService;
            _pavlovServerAdminLogsService = pavlovServerAdminLogsService;
        }


        [HttpGet("[controller]/")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new RconViewModel();
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var isModSomeWhere = await _pavlovServerService.IsModSomeWhere(user, _serverSelectedModsService);
            var servers = await GiveServerWhichTheUserHasRightsTo();

            ViewBag.Servers = servers.Where(x=>x.ServerServiceState==ServerServiceState.active);
            ViewBag.commandsAllow =
                await RightsHandler.GetAllowCommands(viewModel, HttpContext.User, _userservice, isModSomeWhere);

            return View(viewModel);
        }

        private async Task<List<PavlovServer>> GiveServerWhichTheUserHasRightsTo()
        {
            LiteDbUser user;
            user = await _userservice.getUserFromCp(HttpContext.User);
            var servers =
                (await _pavlovServerService.FindAllServerWhereTheUserHasRights(HttpContext.User, user))
                .ToList();
            return servers;
        }

        [HttpPost("[controller]/SendCommandMulti")]
        public async Task<IActionResult> SendCommandMulti(int server, string command, string[] players, string value = "")
        {
            var singleServer = await _pavlovServerService.FindOne(server);
            var servers = await GiveServerWhichTheUserHasRightsTo();
            LiteDbUser user;
            user = await _userservice.getUserFromCp(HttpContext.User);
            if (!servers.Select(x => x.Id).Contains(singleServer.Id))
            {
                return Forbid();
            }
            var isMod = await RightsHandler.IsModOnTheServer(_serverSelectedModsService, singleServer, user.Id);
            var commands = await RightsHandler.GetAllowCommands(new RconViewModel(), HttpContext.User, _userservice, isMod,singleServer,user);

 
            
            var contains = false;
            foreach (var singleCommand in commands)
            {
                if (command.Contains(singleCommand))
                {
                    contains = true;
                }
            }
            if (contains != true)
            {
                return Forbid();
            }
            
            if (command.StartsWith("GodMode"))
            {
                command = "Slap";
                value = "-2147000000";
            }
            if (command.StartsWith("CustomPlayer") || command.StartsWith("Custom"))
            {
                if (command.StartsWith("CustomPlayer"))
                {
                    var count = value.Count(x => x == ' ');
                    if (count == 1)
                    {
                        var pieces = value.Split(new[] { ' ' }, 2);
                        command = pieces[0];
                        value = pieces[1];
                    }
                }
                else
                {
                    command = command.Substring(7);
                }
            }


            var responses = Array.Empty<string>();
            try
            {
                var commandsList = new List<string>();
                foreach (var player in players)
                {
                    if (value == null)
                    {
                        commandsList.Add(command+" "+player);
                    }
                    else
                    {
                        commandsList.Add(command+" "+player+" "+value);
                    }
                }
                
                responses = await RconStatic.SShTunnelMultipleCommands(singleServer, commandsList.ToArray(), _toastifyService); 
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }
            if (string.IsNullOrEmpty(string.Join(";",responses))) return BadRequest("The response was empty!");

            var commandsResulsts = new List<string>();
            try
            {
                
                var commandsListLogs = new List<PavlovServerAdminLogs>();
                foreach (var response in responses)
                {
                    var tmpLog = new PavlovServerAdminLogs
                    {
                        Executor = user,
                        ServerId = server,
                        Time = DateTime.Now
                    };
                    var tmp = JsonConvert.DeserializeObject<MinimumRconResultObject>(response, new JsonSerializerSettings {CheckAdditionalContent = false});
                    var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(response);
                    var value3 = o.Property("")?.Value;
                    var value2 = o.Property(tmp.Command)?.Value;
                    if (tmp != null  && (value3 is {HasValues: false}|| value2 is {HasValues: false}))
                    {
                        tmpLog.CommandResult = tmp.Command;
                        commandsListLogs.Add(tmpLog);
                        commandsResulsts.Add(tmp.Command);
                        if (!tmp.Successful)
                        {
                            await _pavlovServerAdminLogsService.Upsert(commandsListLogs);
                            return new ObjectResult(responses);
                        }
                    }  
                }
                
                
                await _pavlovServerAdminLogsService.Upsert(commandsListLogs);
                return Ok("alreadyNotified! Commands "+string.Join(",",commandsResulsts)+ " where successful.");
            }
            catch (Exception)
            {
                //Ignore only wants to know if its success so we could scip the dialog
            }
            
            return new ObjectResult(responses);
        }

        [HttpPost("[controller]/SendCommand")]
        public async Task<IActionResult> SendCommand(int server, string command)
        {
            var singleServer = await _pavlovServerService.FindOne(server);
            var servers = await GiveServerWhichTheUserHasRightsTo();
            LiteDbUser user;
            user = await _userservice.getUserFromCp(HttpContext.User);
            if (!servers.Select(x => x.Id).Contains(singleServer.Id))
            {
                return Forbid();
            }
            var isMod = await RightsHandler.IsModOnTheServer(_serverSelectedModsService, singleServer, user.Id);
            var commands = await RightsHandler.GetAllowCommands(new RconViewModel(), HttpContext.User, _userservice, isMod,singleServer,user);

            var contains = false;
            foreach (var singleCommand in commands)
            {
                if (command.Contains(singleCommand))
                {
                    contains = true;
                }
            }
            if (contains != true)
            {
                return Forbid();
            }
            
            if (command.StartsWith("GodMode"))
            {
                command = "Slap "+(command.Substring(8, command.Length-8)) + " -2147000000";
            }
            
            if (command.StartsWith("CustomPlayer") || command.StartsWith("Custom"))
            {
                if (command.StartsWith("CustomPlayer"))
                {
                    command = command.Substring(13);
                    var count = command.Count(x => x == ' ');
                    if (count == 2)
                    {
                        var pieces = command.Split(new[] { ' ' }, 3);
                        command = pieces[1] + " " + pieces[0]+ " " + pieces[2]; 
                    }
                    else if(count ==1)
                    {
                        var pieces = command.Split(new[] { ' ' }, 2);
                        command = pieces[1] +" " + pieces[0];
                    }

                }
                else
                {
                    command = command.Substring(7);
                }
            }
            
            var response = "";
            try
            {
                response = await RconStatic.SendCommandSShTunnel(singleServer, command, _toastifyService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }
            if (string.IsNullOrEmpty(response)) return BadRequest("The response was empty!");
            try
            {
                var tmp = JsonConvert.DeserializeObject<MinimumRconResultObject>(response, new JsonSerializerSettings {CheckAdditionalContent = false});
                var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(response);
                var value = o.Property("")?.Value;
                var value2 = o.Property(tmp.Command)?.Value;
                if (tmp != null  && (value is {HasValues: false}|| value2 is {HasValues: false}))
                {
                    if (tmp.Successful)
                    {
                        var commandsListLogs = new List<PavlovServerAdminLogs>();
                        var tmpLog = new PavlovServerAdminLogs
                        {
                            Executor = user,
                            ServerId = server,
                            CommandExecuted = command,
                            CommandResult = tmp.Successful.ToString(),
                        };
                        commandsListLogs.Add(tmpLog);
                        await _pavlovServerAdminLogsService.Upsert(commandsListLogs);
                        return Ok("alreadyNotified! Command "+tmp.Command+ " was successful.");
                    }
                }
            }
            catch (Exception)
            {
                //Ignore only wants to know if its success so we could scip the dialog
            }
            
            return new ObjectResult(response);
        }

        [HttpPost("[controller]/SingleServerInfoPartialView")]
        public async Task<IActionResult> SingleServerInfoPartialView(string server, int serverId)
        {
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
            
            
            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(server.Replace("\"\"", "\"ServerInfo\""));
            if (tmp == null) return BadRequest("Could not Desirialize Object!");
            tmp.ServerId = serverId;
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
            List<Map> listOfMaps;
            listOfMaps = (await _mapsService.FindAll()).ToList();
            if (serverId != null)
            {
                var servers = await GiveServerWhichTheUserHasRightsTo();

                if (!servers.Select(x => x.Id).Contains((int)serverId))
                {
                    return Forbid();
                }
                var server = await _pavlovServerService.FindOne((int) serverId);
                if (server.Shack)
                    listOfMaps = listOfMaps.Where(x => x.Shack && x.ShackSshServerId == server.SshServer.Id).ToList();
                
                var mapsSelected = await _serverSelectedMapService.FindAllFrom(server);
                if (mapsSelected != null)
                {
                    foreach (var map in listOfMaps)
                        if (mapsSelected.FirstOrDefault(x => x.Map?.Id == map.Id) != null)
                            map.sort = 1;
                    listOfMaps = listOfMaps.OrderByDescending(x => x.sort).ToList();
                }
            }

            return PartialView("~/Views/Rcon/PavlovChooseMapPartialView.cshtml", listOfMaps);
        }

        [HttpPost("[controller]/GetBansFromServers")]
        public async Task<IActionResult> GetBansFromServers(int serverId)
        {
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains((int)serverId))
            {
                return Forbid();
            }


            if (serverId <= 0) return BadRequest("Please choose a server!");
            var server = await _pavlovServerService.FindOne(serverId);
            var banlist = Array.Empty<ServerBans>();
            if (server.GlobalBan)
            {
                
                ViewBag.GlobalBan = true;
                banlist = await _serverBansService.FindAllGlobal(true);
            }
            else
            {
                banlist = await _serverBansService.FindAllFromPavlovServerId(serverId, true);
            }

            ViewBag.ServerId = serverId;
            return PartialView("/Views/Rcon/BanList.cshtml", banlist.ToList());
        }

        [HttpPost("[controller]/AddBanPlayer")]
        public async Task<IActionResult> AddBanPlayer(int serverId, string steamId, string timespan)
        {
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
            
            
            
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

            
            LiteDbUser user;
            user = await _userservice.getUserFromCp(HttpContext.User);
            //Get steam name
            try
            {
                var result1 = "";
                try
                {
                    result1 = await RconStatic.SendCommandSShTunnel(ban.PavlovServer, "RefreshList", _toastifyService);
                }
                catch (CommandException e)
                {
                    return BadRequest(e.Message);
                }

                var playersTmp = result1;
                var playersList = JsonConvert.DeserializeObject<PlayerListClass>(playersTmp);
                var playerName = playersList.PlayerList.FirstOrDefault(x => x.UniqueId == steamId)?.Username;
                if (playerName != null) ban.SteamName = playerName;
            }
            catch (Exception)
            {
                //Ignore cause the player name is not something very important
            }

            var result = "";
            try
            {
                result = await RconStatic.SendCommandSShTunnel(ban.PavlovServer, "Ban " + steamId, _toastifyService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            var banlist = (await _serverBansService.FindAllFromPavlovServerId(ban.PavlovServer.Id, true)).ToList();
            try
            {
                banlist = _service.GetServerBansFromBlackList(ban.PavlovServer, banlist);
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
                    _service.SaveBlackListEntry(ban.PavlovServer, banlist);
                }
                catch (CommandException e)
                {
                    return BadRequest(e.Message);
                }
            }

            await _serverBansService.Upsert(ban);

            var tmpList = new List<PavlovServerAdminLogs>();
            tmpList.Add(new PavlovServerAdminLogs
            {
                Executor = user,
                ServerId = serverId,
                CommandExecuted = "ban "+steamId,
                CommandResult = "true"
            });
            await _pavlovServerAdminLogsService.Upsert(tmpList);
            return new ObjectResult(true);
        }
        
        [HttpPost("[controller]/RemoveBanPlayer")]
        public async Task<IActionResult> RemoveBanPlayer(int serverId, string steamId)
        {
            //Todo search for the right server if its a global ban
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
            
            if (serverId <= 0) return BadRequest("Please choose a server!");
            if (string.IsNullOrEmpty(steamId) || steamId == "-") return BadRequest("SteamID must be set!");
            var pavlovServer = await _pavlovServerService.FindOne(serverId);

            LiteDbUser user;
            user = await _userservice.getUserFromCp(HttpContext.User);
            
            
            if (!pavlovServer.GlobalBan) // remove it from this single blacklist file
            {
                
                var banlist = new List<ServerBans>();
                //Remove from blacklist file
                try
                {
                    banlist = _service.GetServerBansFromBlackList(pavlovServer, new List<ServerBans>());
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
                        _service.SaveBlackListEntry(pavlovServer, banlist);
                    }
                    catch (CommandException e)
                    {
                        return BadRequest(e.Message);
                    }
                }
            }
            


            if (!pavlovServer.GlobalBan)
            {
                // remove from Database
                var actualBans = await _serverBansService.FindAllFromPavlovServerId(serverId, true);
                var toRemoveBan = actualBans.FirstOrDefault(x => x.SteamId == steamId);
                if (toRemoveBan != null) await _serverBansService.Delete(toRemoveBan.Id);

                Task.Delay(1000).Wait(); // If you not wait it may just don't work. Don't know why
                //unban command
                try
                {
                    await RconStatic.SendCommandSShTunnel(pavlovServer, "Unban " + steamId, _toastifyService);
                }
                catch (CommandException)
                {
                    return BadRequest("Could not unban player from in memory blacklist!");
                }
            }
            else
            {
                var actualBans = await _serverBansService.FindAllGlobal(true);
                var toRemoveBans = actualBans.Where(x => x.SteamId == steamId);
                foreach (var toRemoveBan in toRemoveBans)
                {
                    await _serverBansService.Delete(toRemoveBan.Id);
                }
            }

            var tmpList = new List<PavlovServerAdminLogs>();
            tmpList.Add(new PavlovServerAdminLogs
            {
                Executor = user,
                ServerId = serverId,
                CommandExecuted = "unban "+steamId,
                CommandResult = "true"
            });
            await _pavlovServerAdminLogsService.Upsert(tmpList);
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
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
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
        
        [HttpPost("[controller]/GetTeamList")]
        public async Task<IActionResult> GetTeamList(int serverId)
        {
            var servers = await GiveServerWhichTheUserHasRightsTo();

            if (!servers.Select(x => x.Id).Contains(serverId))
            {
                return Forbid();
            }
            if (serverId <= 0) return BadRequest("Please choose a server!");

            var server = await _pavlovServerService.FindOne(serverId);
            var players = await _pavlovServerPlayerService.FindAllFromServer(serverId);
            var serverInfo = "";
            var result = "";
            try
            {
                result = await RconStatic.SendCommandSShTunnel(server, "ServerInfo", _toastifyService);
            }
            catch (CommandException e)
            {
                return BadRequest(e.Message);
            }

            serverInfo = result;

            DataBaseLogger.LogToDatabaseAndResultPlusNotify("controlled got serverInfo back: " + serverInfo,
                LogEventLevel.Verbose, _toastifyService);

            var tmp = JsonConvert.DeserializeObject<ServerInfoViewModel>(serverInfo.Replace("\"\"", "\"ServerInfo\""));
            if(tmp!=null)
                tmp.ServerId = serverId;
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