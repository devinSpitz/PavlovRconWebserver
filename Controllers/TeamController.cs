using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Controllers
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly TeamService _teamService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserService _userService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        private readonly UserManager<LiteDbUser> _userManager;
        
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

        public async Task<bool> checkRightsTeamCaptainOrCaptain(int teamId,SteamIdentity steamIdentity = null)
        {
            if (steamIdentity == null)
            {
                return await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userService);
            }
            var steamIdentityOnTeam = new TeamSelectedSteamIdentity();
            if (teamId==0)
            {
                
                steamIdentityOnTeam = await _teamSelectedSteamIdentityService.FindOne(steamIdentity.Id);
            }
            else
            {
                steamIdentityOnTeam = await _teamSelectedSteamIdentityService.FindOne(teamId,steamIdentity.Id);
            }
            if(await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userService))  return true;
            if (steamIdentityOnTeam == null) return false;
            return await RightsHandler.IsUserAtLeastInTeamRole("Captain", steamIdentityOnTeam.RoleOverwrite);
        }
        
        
        public async Task<IActionResult> Index()
        {            
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
            
            if(!await checkRightsTeamCaptainOrCaptain(0,bla)) return Unauthorized();
            
            var teams = new List<Team>();
            var tmpTeams = await _teamService.FindAll(); 

            if (!await RightsHandler.IsUserAtLeastInRole("Captain", HttpContext.User, _userService))
            {
                foreach (var team in tmpTeams)
                {
                    if (await checkRightsTeamCaptainOrCaptain(team.Id, bla))
                    {
                        teams.Add(team);
                    }
                }
            }
            else
            {
                teams = tmpTeams.ToList();
            }
            return View("Index",teams.ToList());
        }
        
        
        [HttpPost]
        public async Task<IActionResult> EditTeam(Team team)
        {
            if (team == null && team.Id != 0)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(team.Id,bla)) return Unauthorized();
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            
            team.AllSteamIdentities = (await _steamIdentityService.FindAll()).ToList(); 
            
            team.TeamSelectedSteamIdentities = (await _teamSelectedSteamIdentityService.FindAllFrom(team.Id)).ToList();
            return View("Team",team);
        }
        
        [HttpGet]
        public async Task<IActionResult> EditSteamIdentity(long? steamIdentityId)
        {

            if (steamIdentityId == null || steamIdentityId == 0)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(0,bla)) return Unauthorized();
                return View("SteamIdentity",new SteamIdentity()
                {
                    LiteDbUsers = _userService.FindAll().ToList()
                });
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }

            
            var steamIdentity = await _steamIdentityService.FindOne((long)steamIdentityId);
            steamIdentity.LiteDbUsers = _userService.FindAll().ToList();
            return View("SteamIdentity",steamIdentity);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSteamIdentity(long steamIdentityId)
        {
            
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            await _steamIdentityService.Delete(steamIdentityId);
            return await SteamIdentitiesIndex();
        }
        
        [HttpGet]
        public async Task<IActionResult> SteamIdentitiesIndex()
        {
            
            if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            var list = (await _steamIdentityService.FindAll()).ToList();
            foreach (var identity in list.Where(identity => !String.IsNullOrEmpty(identity.LiteDbUser.Id.ToString())))
            {
                identity.LiteDbUser = await _userManager.FindByIdAsync(identity.LiteDbUser.Id.ToString());
            }
            return View("SteamIdentities",list);
        }
        
        [HttpPost]
        public async Task<IActionResult> EditSteamIdentity(SteamIdentity steamIdentity)
        {
            if (steamIdentity.Id == 0 || steamIdentity.Id == null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(0,bla)) return Unauthorized();
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            //Handle more stuff can be seen in model
            
            steamIdentity.LiteDbUsers = _userService.FindAll().ToList();
            return View("SteamIdentity",steamIdentity);
        }
        
        
        [HttpPost("[controller]/SaveSteamIdentity")]
        public async Task<IActionResult> SaveSteamIdentity(SteamIdentity steamIdentity)
        {            
            
            steamIdentity.LiteDbUsers = _userService.FindAll().ToList();
            if (steamIdentity.Id == 0 || steamIdentity.Id == null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(0,bla)) return Unauthorized();
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            var newTeam = false;
                steamIdentity.LiteDbUser = _userService.FindAll().FirstOrDefault(x=>x.Id==new ObjectId(steamIdentity.LiteDbUserId));
          
            if(!ModelState.IsValid) 
                return View("SteamIdentity",steamIdentity);
            if (steamIdentity.Id == 0 || steamIdentity.Id == null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(0,bla)) return Unauthorized();
            }
            else
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            await _steamIdentityService.Upsert(steamIdentity);
            

            if (ModelState.ErrorCount > 0)
            {
                return await EditSteamIdentity(steamIdentity);
            }
            
            
            return await Index();
        }
        [HttpGet]
        public async Task<IActionResult> EditTeam(int? teamId)
        {
            if (teamId == 0 || teamId == null)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain((int)teamId,bla)) return Unauthorized();
            }
            if(teamId == null || teamId == 0) 
                return View("Team",new Team());
            
            var team = await _teamService.FindOne((int)teamId);
            return View("Team",team);
        }
        
        
        [HttpPost("[controller]/SaveTeam")]
        public async Task<IActionResult> SaveTeam(Team team)
        {
            if (team.Id == 0 || team.Id == null)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(team.Id,bla)) return Unauthorized();
            }
            
            var newTeam = false;
            if(!ModelState.IsValid) 
                return View("Team",team);

            if (team.Id == null || team.Id == 0)
            {
                var tmp = await _teamService.FindOne(team.Name);
                if (tmp == null)
                {
                    await _teamService.Upsert(team);
                }
                else
                {
                    ModelState.AddModelError("Name", "This name is already used!");
                }
            }
            else
            {
                await _teamService.Upsert(team);
            }
            

            if (ModelState.ErrorCount > 0)
            {
                    return await EditTeam(team);
            }
            
            
            return await Index();
        }
        [HttpGet("[controller]/DeleteTeam/{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            if(!await RightsHandler.IsUserAtLeastInRole("Mod", HttpContext.User, _userService))  return Unauthorized();// have to add that you can see your team if you are not an admin
            await _teamService.Delete(id);
            return await Index();
        }
        
        
        [HttpGet]
        public async Task<IActionResult> EditTeamSelectedSteamIdentities(int teamId)
        {
            if (teamId == 0 || teamId == null)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(teamId,bla)) return Unauthorized();
            }
            var viewModel = new TeamSelectedSteamIdentitiesViewModel();
            viewModel.TeamId = teamId;
            viewModel.SelectedSteamIdentities = (await _teamSelectedSteamIdentityService.FindAllFrom(teamId)).ToList();
            viewModel.AllSteamIdentities = (await _steamIdentityService.FindAll()).ToList();
            return View("TeamSteamIdentitys",viewModel);
        }
        
        [HttpGet("/{teamId}/{steamIdentityId}")]
        public async Task<IActionResult> EditTeamSelectedSteamIdentity(int teamId,long steamIdentityId)
        {
            if (teamId == 0) return BadRequest("team id is required");
            if (steamIdentityId == 0) return BadRequest("steamIdentity id is required");
                var viewModel = new UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel();
            viewModel.teamId = teamId;
            viewModel.steamIdentityId = steamIdentityId;
            return View("TeamSteamIdentity",viewModel);
        }
        
        [HttpGet("/{teamId}/{steamIdentityId}/{overWriteRole}")]
        public async Task<IActionResult> EditTeamSelectedSteamIdentity(int teamId,long steamIdentityId,string overWriteRole)
        {
            if (teamId == 0) return BadRequest("team id is required");
            if (steamIdentityId == 0) return BadRequest("steamIdentity id is required");
            if (String.IsNullOrEmpty(overWriteRole)) return BadRequest("overWriteRole is required");
            var viewModel = new UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel();
            viewModel.teamId = teamId;
            viewModel.steamIdentityId = steamIdentityId;
            viewModel.overWriteRole = overWriteRole;
            return View("TeamSteamIdentity",viewModel);
        }
        
          
        [HttpPost]
        public async Task<IActionResult> UpdateOverwriteRoleOfTeamSelectedSteamIdentity(UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel)
        {
            var identity = await _teamSelectedSteamIdentityService.FindOne(updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.teamId, updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.steamIdentityId);
            if (identity == null) return BadRequest("Did not find the SteamIdentity!");

            identity.RoleOverwrite = updateOverwriteRoleOfTeamSelectedSteamIdentityViewModel.overWriteRole;
            await _teamSelectedSteamIdentityService.Update(identity);
            return await EditTeamSelectedSteamIdentities(identity.Team.Id);
        }
        
        [HttpGet]
        public async Task<IActionResult> SaveTeamSelectedSteamIdentity(int teamId, long steamIdentityId)
        {
            if (teamId == 0 || teamId == null)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(teamId,bla)) return Unauthorized();
            }
            var identity = await _teamSelectedSteamIdentityService.FindOne(teamId, steamIdentityId);
            if (identity != null) return new ObjectResult(true);
            var newIdentity = new TeamSelectedSteamIdentity()
            {
                Team = await _teamService.FindOne(teamId),
                SteamIdentity = await _steamIdentityService.FindOne(steamIdentityId)
            };
            await _teamSelectedSteamIdentityService.Insert(newIdentity);
            return new ObjectResult(true);
        }
        [HttpGet]
        public async Task<IActionResult> DeleteTeamSelectedSteamIdentity(int teamId, long steamIdentityId)
        {
            if (teamId == 0 || teamId == null)
            {
                if (!await RightsHandler.IsUserAtLeastInRole("Admin", HttpContext.User, _userService)) return Unauthorized();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bla = await _steamIdentityService.FindOne(new ObjectId(userId));
                if(!await checkRightsTeamCaptainOrCaptain(teamId,bla)) return Unauthorized();
            }
            var identity = await _teamSelectedSteamIdentityService.FindOne(teamId, steamIdentityId);
            if (identity == null) 
                return await EditTeamSelectedSteamIdentities(teamId);
            await _teamSelectedSteamIdentityService.Delete(identity.Id);
            return await EditTeamSelectedSteamIdentities(teamId);
        }

    }
}