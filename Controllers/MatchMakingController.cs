using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public MatchMakingController(UserService userService,
            MatchService matchService,
            PavlovServerService pavlovServerService,TeamService teamService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentities,
            SteamIdentityService steamIdentityService,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService,
            MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService)
        {
            _userservice = userService;
            _matchService = matchService;
            _pavlovServerService = pavlovServerService;
            _teamService = teamService;
            _steamIdentityService = steamIdentityService;
            _matchSelectedSteamIdentitiesService = matchSelectedSteamIdentities;
            _matchSelectedTeamSteamIdentitiesService = matchSelectedTeamSteamIdentitiesService;
            _teamSelectedSteamIdentityService = teamSelectedSteamIdentityService;
        }


        [HttpGet("[controller]/{showFinished?}")]
        public async Task<IActionResult> Index(bool showFinished = false)
        {
            return showFinished ? View((await _matchService.FindAll()).Where(x=>x.Status!=Status.Finshed)) : View(await _matchService.FindAll());
        }

        
        public async Task<IActionResult> CreateMatch()
        {
            var match = new Match()
            {
                AllTeams = (await _teamService.FindAll()).ToList(),
                AllPavlovServers = (await _pavlovServerService.FindAll()).Where(x=>x.ServerType==ServerType.Event).ToList() // and where no match is already running
                
            };
            return View("Match",match);
        }
        [HttpPost("[controller]/GetAvailableSteamIdentities")]
        public async Task<IActionResult> GetAvailableSteamIdentities(int teamId)
        {
            var steamIdentities = await _teamSelectedSteamIdentityService.FindAllFrom(teamId);
            return new ObjectResult(steamIdentities);
        }
                
        [HttpPost("[controller]/SaveMatch")]
        public async Task<IActionResult> SaveMatch(Match match)
        {
            var bla = await _matchService.Upsert(match);
            if (bla)
            {
                return await Index();
            }
            else
            {
                return await PartialViewPerGameMode(match.GameMode, match);
            }
        }
        
        [HttpPost("[controller]/PartialViewPerGameModeWithId")]
        public async Task<IActionResult> PartialViewPerGameModeWithId(string gameMode,int? matchId)
        {
            var match = new Match();
            if (matchId != null)
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
            var selectedTeam0SteamIdentitiesRaw = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,0)).ToList();
            var selectedTeam1SteamIdentitiesRaw = (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id,1)).ToList();

            var Teams = (await _teamService.FindAll()).ToList();
            
            
            
            var steamIdentities = (await _steamIdentityService.FindAll()).ToList();
            var selectedSteamIdentities = (await _steamIdentityService.FindAList(selectedSteamIdentitiesRaw.Select(x=>x.Id).ToList())).ToList();
            var selectedTeam0SteamIdentities = (await _steamIdentityService.FindAList(selectedTeam0SteamIdentitiesRaw.Select(x=>x.Id).ToList())).ToList();
            var selectedTeam1SteamIdentities = (await _steamIdentityService.FindAList(selectedTeam1SteamIdentitiesRaw.Select(x=>x.Id).ToList())).ToList();
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