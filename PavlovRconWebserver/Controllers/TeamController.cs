using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Hangfire.Annotations;
using LiteDB;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize(Roles = CustomRoles.User)]
    public class TeamController : Controller
    {
        private readonly SteamIdentityService _steamIdentityService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly TeamService _teamService;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly UserService _userService;

        public TeamController(TeamService teamService,
            SteamIdentityService steamIdentityService,
            UserService userService,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService,
            UserManager<LiteDbUser> userManager)
        {
            _teamService = teamService;
            _steamIdentityService = steamIdentityService;
            _userService = userService;
            _teamSelectedSteamIdentityService = teamSelectedSteamIdentityService;
            _userManager = userManager;
        }
        


        public async Task<IActionResult> Index()
        {
            
            var user = await _userService.getUserFromCp(HttpContext.User);
            var servers = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            

            return View("Index", servers.ToList());
        }


        [HttpPost]
        public async Task<IActionResult> EditTeam(Team team)
        {
            if (team == null) return BadRequest("please set a Team");
            
            var user = await _userService.getUserFromCp(HttpContext.User);
            var servers = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (team.Id != 0)
            {
                if (!servers.Select(x => x.Id).Contains(team.Id))
                {
                    return Forbid();
                }
            }

            team.AllSteamIdentities = (await _steamIdentityService.FindAll()).ToList();

            team.TeamSelectedSteamIdentities = (await _teamSelectedSteamIdentityService.FindAllFrom(team.Id)).ToList();
            return View("Team", team);
        }

        [Authorize(Roles = CustomRoles.Mod)]
        [HttpGet]
        public async Task<IActionResult> EditSteamIdentity([CanBeNull] string steamIdentityId)
        {
            if (steamIdentityId == null || steamIdentityId == "0")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                return View("SteamIdentity", new SteamIdentity
                {
                    LiteDbUsers = (await _userService.FindAll()).ToList()
                });
            }


            var steamIdentity = await _steamIdentityService.FindOne(steamIdentityId);
            steamIdentity.LiteDbUsers = (await _userService.FindAll()).ToList();
            return View("SteamIdentity", steamIdentity);
        }

        [Authorize(Roles = CustomRoles.Mod)]
        [HttpGet]
        public async Task<IActionResult> DeleteSteamIdentity(string steamIdentityId)
        {
            await _steamIdentityService.Delete(steamIdentityId);
            return await SteamIdentitiesIndex();
        }

        [Authorize(Roles = CustomRoles.Mod)]
        [HttpGet]
        public async Task<IActionResult> SteamIdentitiesIndex()
        {
            var list = (await _steamIdentityService.FindAll()).ToList();
            foreach (var identity in list.Where(identity =>identity.LiteDbUser != null &&  !string.IsNullOrEmpty(identity.LiteDbUser.Id.ToString())))
                identity.LiteDbUser = await _userManager.FindByIdAsync(identity.LiteDbUser.Id.ToString());
            return View("SteamIdentities", list);
        }

        [Authorize(Roles = CustomRoles.Mod)]
        [HttpPost]
        public async Task<IActionResult> EditSteamIdentity(SteamIdentity steamIdentity)
        {
            steamIdentity.LiteDbUsers = (await _userService.FindAll()).ToList();
            return View("SteamIdentity", steamIdentity);
        }


        [HttpGet("[controller]/EditOwnSteamIdentity")]
        public async Task<IActionResult> EditOwnSteamIdentity()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
            if (bla == null)
                bla = new SteamIdentity
                {
                    LiteDbUserId = userId,
                    LiteDbUser = (await _userService.FindAll()).FirstOrDefault(x => x.Id == new ObjectId(userId))
                };
            //Handle more stuff can be seen in model

            ViewBag.IsOwnSteamIdentity = true;

            return View("SteamIdentity", bla);
        }
        
        [HttpGet("[controller]/AddSteamIdentityView")]
        public async Task<IActionResult> AddSteamIdentityView()
        {
            ViewBag.IsAddSteamIdentity = true;

            return View("SteamIdentity", new SteamIdentity());
        }

        [HttpPost("[controller]/AddSteamIdentity")]
        public async Task<IActionResult> AddSteamIdentity(SteamIdentity steamIdentity)
        {
            
            var exist = await _steamIdentityService.FindOne(steamIdentity.Id);
            if (exist != null) return BadRequest("This steamId already exist and your only allow to add not to edit!");

            //security so that the LiteDbUser not can be set here
            steamIdentity.LiteDbUser = null;
            await _steamIdentityService.Insert(steamIdentity);
            //Handle more stuff can be seen in model
            return await Index();
        }
        

        [HttpPost("[controller]/SaveSteamIdentity")]
        public async Task<IActionResult> SaveSteamIdentity(SteamIdentity steamIdentity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
            var hisOwn = userId == steamIdentity.LiteDbUserId;
            var currentOwner = await _steamIdentityService.FindOne(steamIdentity.Id);
            steamIdentity.LiteDbUsers = (await _userService.FindAll()).ToList();
            if (userId != steamIdentity.LiteDbUserId)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Mod", HttpContext.User, _userService))
                    return Unauthorized();
            }

            ViewBag.IsOwnSteamIdentity = hisOwn;
            steamIdentity.LiteDbUser = (await _userService.FindAll())
                .FirstOrDefault(x => x.Id == new ObjectId(steamIdentity.LiteDbUserId));

            if (!ModelState.IsValid)
                return View("SteamIdentity", steamIdentity);

            if (userId != steamIdentity.LiteDbUserId)
                if (!await RightsHandler.IsUserAtLeastInRole("Mod", HttpContext.User, _userService))
                    return Unauthorized();

            if (currentOwner == null || steamIdentity.LiteDbUserId == currentOwner.LiteDbUser?.Id.ToString()||currentOwner.LiteDbUser==null)
                await _steamIdentityService.Upsert(steamIdentity);
            else
                return BadRequest("That would be a duplicate entry!");


            if (ModelState.ErrorCount > 0) return await EditSteamIdentity(steamIdentity);

            if (hisOwn)
                return RedirectToAction("Index", "Manage");
            return await Index();
        }

        [HttpGet]
        public async Task<IActionResult> EditTeam(int? teamId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (teamId == 0 || teamId == null)
            {
                return View("Team", new Team());
            }
            else
            {
                if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains((int)teamId))
                {
                    return Forbid();
                }
            }

            return View("Team", teams.FirstOrDefault(x=>x.Id==teamId));
        }


        [HttpPost("[controller]/SaveTeam")]
        public async Task<IActionResult> SaveTeam(Team team)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if(team.Id != 0)
            {
                if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(team.Id))
                {
                    return Forbid();
                }
            }

            if (!ModelState.IsValid)
                return View("Team", team);

            if (team.Id == 0)
            {
                
                var tmp = await _teamService.FindOne(team.Name);
                if (tmp == null)
                {
                    var own = await _steamIdentityService.FindOne(user.Id);
                    if (own == null)
                    {
                        ModelState.AddModelError("Name", "You need a steamIdentity to create a team!");
                        return await EditTeam(team);
                    }
                    await _teamService.Upsert(team);

                
                    await _teamSelectedSteamIdentityService.Insert(new TeamSelectedSteamIdentity
                    {
                        SteamIdentity = own,
                        RoleOverwrite = "Admin",
                        Team = team
                    });
                }
                else
                    ModelState.AddModelError("Name", "This name is already used!");
            }
            else
            {
                await _teamService.Upsert(team);
            }


            if (ModelState.ErrorCount > 0) return await EditTeam(team);


            return await Index();
        }

        [HttpGet("[controller]/DeleteTeam/{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(id))
            {
                return Forbid();
            }
            await _teamService.Delete(id);
            return await Index();
        }


        [HttpGet]
        public async Task<IActionResult> EditTeamSelectedSteamIdentities(int teamId)
        {            
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod"||x.TeamRole=="Captain").Select(x => x.Id).Contains(teamId))
            {
                return Forbid();
            }

            var viewModel = new TeamSelectedSteamIdentitiesViewModel();
            viewModel.TeamId = teamId;
            viewModel.SelectedSteamIdentities = (await _teamSelectedSteamIdentityService.FindAllFrom(teamId)).ToList();
            viewModel.AllSteamIdentities = (await _steamIdentityService.FindAll()).ToList();
            return View("TeamSteamIdentitys", viewModel);
        }

        [HttpGet("[controller]/{teamId}/{steamIdentityId}")]
        public async Task<IActionResult> EditTeamSelectedSteamIdentity(int teamId, string steamIdentityId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod"||x.TeamRole=="Captain").Select(x => x.Id).Contains(teamId))
            {
                return Forbid();
            }
            if (teamId == 0) return BadRequest("team id is required");
            if (steamIdentityId == "0") return BadRequest("steamIdentity id is required");
            var viewModel = new UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel
            {
                teamId = teamId, steamIdentityId = steamIdentityId
            };
            return View("TeamSteamIdentity", viewModel);
        }

        [HttpGet("[controller]/{teamId}/{steamIdentityId}/{overWriteRole}")]
        public async Task<IActionResult> EditTeamSelectedSteamIdentity(int teamId, string steamIdentityId,
            string overWriteRole)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(teamId))
            {
                return Forbid();
            }
            if (teamId == 0) return BadRequest("team id is required");
            if (steamIdentityId == "0") return BadRequest("steamIdentity id is required");
            if (string.IsNullOrEmpty(overWriteRole)) return BadRequest("overWriteRole is required");
            var viewModel = new UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel
            {
                teamId = teamId, steamIdentityId = steamIdentityId, overWriteRole = overWriteRole
            };
            return View("TeamSteamIdentity", viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateOverwriteRoleOfTeamSelectedSteamIdentity(
            UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel
                updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.teamId))
            {
                return Forbid();
            }
            var identity = await _teamSelectedSteamIdentityService.FindOne(
                updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.teamId,
                updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.steamIdentityId);
            if (identity == null) return BadRequest("Did not find the SteamIdentity!");

            identity.RoleOverwrite = updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.overWriteRole;
            await _teamSelectedSteamIdentityService.Update(identity);
            return await EditTeamSelectedSteamIdentities(identity.Team.Id);
        }

        [HttpGet]
        public async Task<IActionResult> SaveTeamSelectedSteamIdentity(int teamId, string steamIdentityId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(teamId))
            {
                return Forbid();
            }

            var identity = await _teamSelectedSteamIdentityService.FindOne(teamId, steamIdentityId);
            if (identity != null) return new ObjectResult(true);
            var newIdentity = new TeamSelectedSteamIdentity
            {
                Team = await _teamService.FindOne(teamId),
                SteamIdentity = await _steamIdentityService.FindOne(steamIdentityId)
            };
            await _teamSelectedSteamIdentityService.Insert(newIdentity);
            return new ObjectResult(true);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTeamSelectedSteamIdentity(int teamId, string steamIdentityId)
        {
            var user = await _userService.getUserFromCp(HttpContext.User);
            var teams = await _teamService.FindAllTeamsWhereTheUserHasRights(HttpContext.User,user);
            if (!teams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Mod").Select(x => x.Id).Contains(teamId))
            {
                return Forbid();
            }

            var identity = await _teamSelectedSteamIdentityService.FindOne(teamId, steamIdentityId);
            if (identity == null)
                return await EditTeamSelectedSteamIdentities(teamId);
            await _teamSelectedSteamIdentityService.Delete(identity.Id);
            return await EditTeamSelectedSteamIdentities(teamId);
        }
    }
}