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
    
    [Authorize(Roles = CustomRoles.User)]
    public class MatchMakingController : Controller
    {
        private readonly MapsService _mapsService;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;
        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly MatchService _matchService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly PublicViewListsService _publicViewListsService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly TeamService _teamService;
        private readonly UserService _userservice;

        public MatchMakingController(UserService userService,
            MatchService matchService,
            PavlovServerService pavlovServerService, TeamService teamService,
            MatchSelectedSteamIdentitiesService matchSelectedSteamIdentities,
            SteamIdentityService steamIdentityService,
            MapsService mapsService,
            PublicViewListsService publicViewListsService,
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
            _mapsService = mapsService;
            _publicViewListsService = publicViewListsService;
        }


        [HttpGet("[controller]/{showFinished?}")]
        public async Task<IActionResult> Index(bool showFinished = false)
        {
            
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            return showFinished
                ? View(servers)
                : View(servers.Where(x => x.Status != Status.Finshed));
        }

        [HttpGet]
        public async Task<IActionResult> EditMatchResult(int id)
        {
            
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match like that exists!");
            if (!match.isFinished()) return BadRequest("Match is not finished jet!");
            if (match.EndInfo == null) return BadRequest("Match has no endInfo");
            var map = await _mapsService.FindOne(match.MapId.Replace("UGC", ""));
            if (map == null) return BadRequest("Match has no map set");
            var result = _publicViewListsService.PavlovServerPlayerListPublicViewModel(new PavlovServerInfo
            {
                MapLabel = match.MapId,
                MapPictureLink = map.ImageUrl,
                GameMode = match.EndInfo.GameMode,
                ServerName = match.EndInfo.ServerName,
                RoundState = match.EndInfo.RoundState,
                PlayerCount = match.EndInfo.PlayerCount,
                Teams = match.EndInfo.Teams,
                Team0Score = match.EndInfo.Team0Score,
                Team1Score = match.EndInfo.Team1Score,
                ServerId = match.PavlovServer.Id
            }, match.PlayerResults);
            result.MatchId = match.Id;
            return View("EditResult", result);
        }

        [HttpPost("[controller]/SaveMatchResult")]
        public async Task<IActionResult> SaveMatchResult(PavlovServerPlayerListPublicViewModel match)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(match.MatchId))
            {
                return Forbid();
            }
            var realMatch = await _matchService.FindOne(match.MatchId);
            if (realMatch == null) return BadRequest("No match like that exists!");

            await _matchService.SaveMatchResult(match, realMatch);

            return RedirectToAction("Index", "MatchMaking");
        }


        [HttpGet("[controller]/EditMatch/{id}")]
        public async Task<IActionResult> EditMatch(int id)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            var oldMatch = await _matchService.FindOne(id);
            if (!oldMatch.isEditable()) return BadRequest("No meets requirement!");
            var match = await _matchService.PrepareViewModel(oldMatch);

            return View("Match", match);
        }

        
        [Authorize(Roles = CustomRoles.AnyOtherThanUser)]
        [HttpGet("[controller]/CreateMatch/")]
        public async Task<IActionResult> CreateMatch()
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var match = new MatchViewModel
            {
                AllTeams = (await _teamService.FindAll()).ToList(),
                AllPavlovServers = (await _pavlovServerService.FindAllServerWhereTheUserHasRights(HttpContext.User,user)).Where(x => x.ServerType == ServerType.Event)
                    .ToList() // and where no match is already running
            };
            return View("Match", match);
        }

        [HttpPost("[controller]/GetAvailableSteamIdentities")]
        public async Task<IActionResult> GetAvailableSteamIdentities(int teamId, int? matchId)
        {
            var steamIdentities = await _teamSelectedSteamIdentityService.FindAllFrom(teamId);

            var usedSteamIdentities = new List<MatchSelectedSteamIdentity>();
            if (matchId != null)
                usedSteamIdentities =
                    (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch((int) matchId)).ToList();
            else
                usedSteamIdentities =
                    (await _matchSelectedSteamIdentitiesService.FindAll()).ToList();


            var list = steamIdentities
                .Where(x => !usedSteamIdentities.Select(y => y.SteamIdentityId).Contains(x.SteamIdentity.Id)).ToList();
            var result = new JsonResult(list);
            return result;
        }

        [HttpGet("[controller]/ForceStartMatch")]
        public async Task<IActionResult> ForceStartMatch(int id)
        {
            
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            if (!match.isForceStartable()) return BadRequest("No meets requirement!");
            match.ForceStart = true;
            await _matchService.Upsert(match);
            return RedirectToAction("Index", "MatchMaking");
        }

        [HttpGet("[controller]/StartMatch")]
        public async Task<IActionResult> StartMatch(int id)
        {            
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            if (!match.isStartable()) return BadRequest("No meets requirement!");
            await _matchService.StartMatch(id);
            return RedirectToAction("Index", "MatchMaking");
        }

        [HttpGet("[controller]/ForceSopMatch")]
        public async Task<IActionResult> ForceStopMatch(int id)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            var match = await _matchService.FindOne(id);
            if (match == null) return BadRequest("No match found!");
            if (!match.isForceStopatable()) return BadRequest("No meets requirement!");
            match.ForceSop = true;
            await _matchService.Upsert(match);
            return RedirectToAction("Index", "MatchMaking");
        }

        [Authorize(Roles = CustomRoles.AnyOtherThanUser)]
        [HttpPost("[controller]/SaveMatch")]
        public async Task<IActionResult> SaveMatch(MatchViewModel match)
        {

            var realmatch = new Match();
            // make from viewmodel right model
            if (match.Id != 0) //edit or new
            {
                var user = await _userservice.getUserFromCp(HttpContext.User);
                var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
                if (!servers.Select(x => x.Id).Contains(match.Id))
                {
                    return Forbid();
                }
                realmatch = await _matchService.FindOne(match.Id);
                if (realmatch.Status != Status.Preparing)
                    return BadRequest("The match already started so you can not change anything!");
            }

            if (await _matchService.SaveMatchToService(match, realmatch)) return new ObjectResult(true);

            return BadRequest("Could not save match! Internal Error!");
        }

        [HttpPost("[controller]/PartialViewPerGameModeWithId")]
        public async Task<IActionResult> PartialViewPerGameModeWithId(string gameMode, int? matchId)
        {
            var match = new Match();
            if (matchId != null && matchId != 0) match = await _matchService.FindOne((int) matchId);
            return await PartialViewPerGameMode(gameMode, match);
        }

        [HttpGet("[controller]/Delete")]
        public async Task<IActionResult> Delete(int Id)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (!servers.Select(x => x.Id).Contains(Id))
            {
                return Forbid();
            }
            if (await _matchService.CanBeDeleted(Id))
            {
                await _matchService.Delete(Id);
                return RedirectToAction("Index", "MatchMaking");
            }

            return RedirectToAction("Index", "MatchMaking");
        }

        //Todo: Most of it to the service?
        [HttpPost("[controller]/PartialViewPerGameMode")]
        public async Task<IActionResult> PartialViewPerGameMode(string gameMode, Match match)
        {
            var user = await _userservice.getUserFromCp(HttpContext.User);
            var servers = await _matchService.FindAllMatchesWhereTheUserHasRights(HttpContext.User,user);
            
            if (match.Id!=0&&!servers.Select(x => x.Id).Contains(match.Id))
            {
                return Forbid();
            }
            var selectedSteamIdentitiesRaw =
                (await _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(match.Id)).ToList();
            var selectedTeam0SteamIdentitiesRaw =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id, 0)).ToList();
            var selectedTeam1SteamIdentitiesRaw =
                (await _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(match.Id, 1)).ToList();

            var Teams = (await _teamService.FindAll()).ToList();


            var steamIdentities = (await _steamIdentityService.FindAll()).ToList();
            var selectedSteamIdentities =
                (await _steamIdentityService.FindAList(selectedSteamIdentitiesRaw.Select(x => x.SteamIdentityId)
                    .ToList())).ToList();
            var selectedTeam0SteamIdentities =
                (await _steamIdentityService.FindAList(selectedTeam0SteamIdentitiesRaw.Select(x => x.SteamIdentityId)
                    .ToList())).ToList();
            var selectedTeam1SteamIdentities =
                (await _steamIdentityService.FindAList(selectedTeam1SteamIdentitiesRaw.Select(x => x.SteamIdentityId)
                    .ToList())).ToList();
            foreach (var selectedSteamIdentity in selectedSteamIdentities)
                steamIdentities.Remove(steamIdentities.FirstOrDefault(x => x.Id == selectedSteamIdentity.Id));
            var gotAnswer = GameModes.HasTeams.TryGetValue(gameMode, out var hasTeams);
            if (gotAnswer)
            {
                if (hasTeams)
                {
                    var gotAnswer2 = GameModes.OneTeam.TryGetValue(gameMode, out var oneTeam);
                    if (gotAnswer2)
                    {
                        if (oneTeam)
                            return PartialView("SteamIdentityPartialView", new SteamIdentityMatchViewModel
                            {
                                SelectedSteamIdentities = selectedSteamIdentities,
                                AllSteamIdentities = steamIdentities
                            });
                        return PartialView("TeamPartailView", new SteamIdentityMatchTeamViewModel
                        {
                            selectedTeam0 = match.Team0?.Id,
                            selectedTeam1 = match.Team1?.Id,
                            AvailableTeams = Teams,
                            SelectedSteamIdentitiesTeam0 = selectedTeam0SteamIdentities,
                            SelectedSteamIdentitiesTeam1 = selectedTeam1SteamIdentities
                        });
                    }

                    BadRequest("internal error!");
                }
                else
                {
                    return PartialView("SteamIdentityPartialView", new SteamIdentityMatchViewModel
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