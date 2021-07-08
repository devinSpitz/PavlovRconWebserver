using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    
    [Authorize]
    public class MatchMakingController : Controller
    {
        private readonly UserService _userservice;
        private readonly MatchService _matchService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly TeamService _teamService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;
        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly IConfiguration _configuration;
        public MatchMakingController(UserService userService,
            MatchService matchService,
            PavlovServerService pavlovServerService,TeamService teamService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentities,
            SteamIdentityService steamIdentityService,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService,
            IConfiguration config)
        {
            _userservice = userService;
            _matchService = matchService;
            _pavlovServerService = pavlovServerService;
            _teamService = teamService;
            _steamIdentityService = steamIdentityService;
            _matchSelectedSteamIdentitiesService = matchSelectedSteamIdentities;
            _matchSelectedTeamSteamIdentitiesService = matchSelectedTeamSteamIdentitiesService;
            _teamSelectedSteamIdentityService = teamSelectedSteamIdentityService;
            _configuration = config;
        }


        [HttpGet("[controller]/{showFinished?}")]
        public async Task<IActionResult> Index(bool showFinished = false)
        {
            return showFinished ? View((await _matchService.FindAll()).Where(x=>x.Status!=Status.Finshed)) : View(await _matchService.FindAll());
        }

        [HttpGet]
        public async Task<IActionResult> EditMatch(int id)
        {
            var oldMatch = await _matchService.FindOne(id);
            var match = new MatchViewModel
            {
                Id = oldMatch.Id,
                Name = oldMatch.Name,
                MapId = oldMatch.MapId,
                ForceSop = oldMatch.ForceSop,
                ForceStart = oldMatch.ForceStart,
                TimeLimit = oldMatch.TimeLimit,
                PlayerSlots = oldMatch.PlayerSlots,
                GameMode = oldMatch.GameMode,
                Team0 = oldMatch.Team0,
                Team1 = oldMatch.Team1,
                PavlovServer = oldMatch.PavlovServer,
                Status = (Status) oldMatch.Status,
                Team0Id = oldMatch.Team0?.Id,
                Team1Id = oldMatch.Team1?.Id,
            };
            if (oldMatch.PavlovServer != null)
                match.PavlovServerId = oldMatch.PavlovServer.Id;
            match.AllTeams = (await _teamService.FindAll()).ToList();
            match.AllPavlovServers = (await _pavlovServerService.FindAll()).Where(x => x.ServerType == ServerType.Event)
                .ToList(); // and where no match is already running

            match.MatchSelectedSteamIdentities =
                (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(oldMatch.Id)).ToList();
            match.MatchTeam0SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(oldMatch.Id, 0)).ToList();
            match.MatchTeam1SelectedSteamIdentities =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(oldMatch.Id, 1)).ToList();
            
            var test = (await _matchSelectedTeamSteamIdentitiesService.FindAll()).ToList();
            return View("Match",match);
        }
        
        public async Task<IActionResult> CreateMatch()
        {
            var match = new MatchViewModel()
            {
                AllTeams = (await _teamService.FindAll()).ToList(),
                AllPavlovServers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Event).ToList() // and where no match is already running
                
            };
            return View("Match",match);
        }
        [HttpPost("[controller]/GetAvailableSteamIdentities")]
        public async Task<IActionResult> GetAvailableSteamIdentities(int teamId,int? matchId)
        {
            var steamIdentities = await _teamSelectedSteamIdentityService.FindAllFrom(teamId);
            
            var usedSteamIdentities = new List<MatchSelectedSteamIdentity>();
            if (matchId!=null)
            {
                usedSteamIdentities =
                (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch((int)matchId)).ToList();
                
            }
            usedSteamIdentities =
                (await _matchSelectedSteamIdentitiesService.FindAll()).ToList();

            
            var list = steamIdentities.Where(x=>!usedSteamIdentities.Select(y=>y.SteamIdentityId).Contains(x.SteamIdentity.Id)).ToList();
            var result = new JsonResult(list);
            return result;
        }
        
        [HttpGet("[controller]/ForceStartMatch")]
        public async Task<IActionResult> ForceStartMatch(int id)
        {
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            match.ForceStart = true;
            await _matchService.Upsert(match);
            return RedirectToAction("Index","MatchMaking");
        }       
        [HttpGet("[controller]/StartMatch")]
        public async Task<IActionResult> StartMatch(int id)
        {
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            await _matchService.StartMatch(id,_configuration.GetConnectionString("DefaultConnection"));
            return RedirectToAction("Index","MatchMaking");
        }     
        [HttpGet("[controller]/ForceSopMatch")]
        public async Task<IActionResult> ForceStopMatch(int id)
        {
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            match.ForceSop = true;
            await _matchService.Upsert(match);
            return RedirectToAction("Index","MatchMaking");
        }
                
        [HttpPost("[controller]/SaveMatch")]
        public async Task<IActionResult> SaveMatch(MatchViewModel match)
        {
            var realmatch = new Match();
            // make from viewmodel right model
            if (match.Id != 0 && match.Id != null) //edit or new
            {
                realmatch = await _matchService.FindOne(match.Id);
                if (realmatch.Status != Status.Preparing)
                {
                    return BadRequest("The match already started so you can not change anything!");
                }
            }

            realmatch.Name = match.Name;
            realmatch.MapId = match.MapId;
            realmatch.GameMode = match.GameMode;
            realmatch.TimeLimit = match.TimeLimit;
            realmatch.PlayerSlots = match.PlayerSlots;
            
            var gotAnswer = GameModes.HasTeams.TryGetValue(realmatch.GameMode, out var hasTeams);
            if (gotAnswer)
            {
                if (hasTeams)
                {
                    realmatch.Team0 = await _teamService.FindOne((int)match.Team0Id);
                    realmatch.Team0.TeamSelectedSteamIdentities =
                        (await _teamSelectedSteamIdentityService.FindAllFrom(realmatch.Team0.Id)).ToList();
                    realmatch.Team1 = await _teamService.FindOne((int)match.Team1Id);
                    realmatch.Team1.TeamSelectedSteamIdentities =
                        (await _teamSelectedSteamIdentityService.FindAllFrom(realmatch.Team1.Id)).ToList();
                    
                    // Check all steam identities
                    foreach (var team0SelectedSteamIdentity in match.MatchTeam0SelectedSteamIdentitiesStrings)
                    {
                        var tmp = realmatch.Team0.TeamSelectedSteamIdentities.FirstOrDefault(x =>
                            x.SteamIdentity.Id.ToString() == team0SelectedSteamIdentity);
                        if (tmp != null)
                        {
                            realmatch.MatchTeam0SelectedSteamIdentities.Add(new MatchTeamSelectedSteamIdentity()
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = team0SelectedSteamIdentity,
                                TeamId = 0
                            });
                        }
                        else
                        {
                            return BadRequest("The SteamID:"+team0SelectedSteamIdentity+" is not a member of the team0!");
                        }
                    }
                    
                    foreach (var team1SelectedSteamIdentity in match.MatchTeam1SelectedSteamIdentitiesStrings)
                    {
                        var tmp = realmatch.Team1.TeamSelectedSteamIdentities.FirstOrDefault(x =>
                            x.SteamIdentity.Id.ToString() == team1SelectedSteamIdentity);
                        if (tmp != null)
                        {
                            realmatch.MatchTeam0SelectedSteamIdentities.Add(new MatchTeamSelectedSteamIdentity()
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = team1SelectedSteamIdentity,
                                TeamId = 1
                            });
                        }
                        else
                        {
                            return BadRequest("The SteamID:"+team1SelectedSteamIdentity+" is not a member of the team0!");
                        }
                    }
                }
                else
                {
                    foreach (var SelectedSteamIdentity in match.MatchSelectedSteamIdentitiesStrings)
                    {
                        var tmp = await _steamIdentityService.FindOne(SelectedSteamIdentity);
                        if (tmp != null)
                        {
                            realmatch.MatchSelectedSteamIdentities.Add(new MatchSelectedSteamIdentity()
                            {
                                matchId = realmatch.Id,
                                SteamIdentityId = SelectedSteamIdentity
                            });
                        }
                        else
                        {
                            return BadRequest("The SteamID:"+SelectedSteamIdentity+" is not registred!");
                        }
                    }
                }
                //When not a server is set!!!! or server already is running a match
                if (match.PavlovServerId <= 0 || match.PavlovServerId == null)
                {
                    return BadRequest("Please select a server!");
                }
                realmatch.PavlovServer = await _pavlovServerService.FindOne(match.PavlovServerId);
                if(realmatch.PavlovServer == null) return BadRequest("Error while mapping pavlovServer!");
                realmatch.Status = match.Status;
            }
            else
            {
                return BadRequest("Could not cast GameMode!");
            }


            //
            
            var bla = await _matchService.Upsert(realmatch);
            
            if (bla)
            {            
                // First remove Old TeamSelected and Match selected stuff

                if (realmatch.MatchSelectedSteamIdentities.Count > 0)
                {
                    foreach (var realmatchMatchSelectedSteamIdentity in realmatch.MatchSelectedSteamIdentities)
                    {
                        realmatchMatchSelectedSteamIdentity.matchId = realmatch.Id;
                    }

                    await _matchSelectedSteamIdentitiesService.Upsert(realmatch.MatchSelectedSteamIdentities, realmatch.Id);
                }
            
                // Then write the new ones
            
                if(realmatch.MatchTeam0SelectedSteamIdentities.Count>0)
                {
                    foreach (var matchTeam0SelectedSteamIdentities in realmatch.MatchTeam0SelectedSteamIdentities)
                    {
                        matchTeam0SelectedSteamIdentities.matchId = realmatch.Id;
                    }

                    await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam0SelectedSteamIdentities, realmatch.Id, (int)match.Team0Id);
                }
                
                if(realmatch.MatchTeam1SelectedSteamIdentities.Count>0)
                {
                    foreach (var matchTeam1SelectedSteamIdentities in realmatch.MatchTeam1SelectedSteamIdentities)
                    {
                        matchTeam1SelectedSteamIdentities.matchId = realmatch.Id;
                    }

                    await _matchSelectedTeamSteamIdentitiesService.Upsert(realmatch.MatchTeam1SelectedSteamIdentities, realmatch.Id, (int)match.Team1Id);
                }
                
                return new ObjectResult(true);
            }
            else
            {
                return BadRequest("Could not save match! Internal Error!");
            }
        }
        
        [HttpPost("[controller]/PartialViewPerGameModeWithId")]
        public async Task<IActionResult> PartialViewPerGameModeWithId(string gameMode,int? matchId)
        {
            var match = new Match();
            if (matchId != null && matchId != 0)
            {
                match = await _matchService.FindOne((int)matchId);
            }
            return await PartialViewPerGameMode(gameMode, match);
        }   
        
        [HttpPost("[controller]/PartialViewPerGameMode")]
        public async Task<IActionResult> PartialViewPerGameMode(string gameMode,Match match)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("User", HttpContext.User, _userservice))  return Unauthorized();

            var selectedSteamIdentitiesRaw = (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(match.Id)).ToList();
            var selectedSteamIdentitiesRaw2 = (await _matchSelectedSteamIdentitiesService.FindAll()).ToList();
            var selectedTeam0SteamIdentitiesRaw = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,0)).ToList();
            var selectedTeam1SteamIdentitiesRaw = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,1)).ToList();

            var Teams = (await _teamService.FindAll()).ToList();
            
            
            
            var steamIdentities = (await _steamIdentityService.FindAll()).ToList();
            var selectedSteamIdentities = (await _steamIdentityService.FindAList(selectedSteamIdentitiesRaw.Select(x=>x.SteamIdentityId).ToList())).ToList();
            var selectedTeam0SteamIdentities = (await _steamIdentityService.FindAList(selectedTeam0SteamIdentitiesRaw.Select(x=>x.SteamIdentityId).ToList())).ToList();
            var selectedTeam1SteamIdentities = (await _steamIdentityService.FindAList(selectedTeam1SteamIdentitiesRaw.Select(x=>x.SteamIdentityId).ToList())).ToList();
            foreach (var selectedSteamIdentity in selectedSteamIdentities)
            {
                steamIdentities.Remove(steamIdentities.FirstOrDefault(x => x.Id == selectedSteamIdentity.Id));
            }
            var gotAnswer = GameModes.HasTeams.TryGetValue(gameMode, out var hasTeams);
            if (gotAnswer)
            {
                if (hasTeams)
                {
                    var gotAnswer2 = GameModes.OneTeam.TryGetValue(gameMode, out var oneTeam);
                    if (gotAnswer2)
                    {
                        if (oneTeam)
                        {
                            return PartialView("SteamIdentityPartialView",new SteamIdentityMatchViewModel()
                            {
                                SelectedSteamIdentities = selectedSteamIdentities,
                                AllSteamIdentities = steamIdentities
                            }); 
                        }
                        else
                        {
                            return PartialView("TeamPartailView", new SteamIdentityMatchTeamViewModel
                            {
                                selectedTeam0 = match.Team0?.Id,
                                selectedTeam1 = match.Team1?.Id,
                                AvailableTeams = Teams,
                                SelectedSteamIdentitiesTeam0 = selectedTeam0SteamIdentities,
                                SelectedSteamIdentitiesTeam1 = selectedTeam1SteamIdentities
                            });
                        }
                        
                    }
                    
                    BadRequest("internal error!");
                }
                else
                { 
                    return PartialView("SteamIdentityPartialView",new SteamIdentityMatchViewModel()
                    {
                        SelectedSteamIdentities = selectedSteamIdentities,
                        AllSteamIdentities = steamIdentities
                    });
                }
            }
            else
            {
                return BadRequest("There is no gameMode like that!");
            }
            return BadRequest("There is no gameMode like that!"); 
        }
        
        
    }
}