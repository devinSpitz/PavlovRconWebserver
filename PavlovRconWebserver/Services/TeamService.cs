using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly TeamSelectedSteamIdentityService _selectedSteamIdentityService;


        public TeamService(ILiteDbIdentityAsyncContext liteDbContext,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService)
        {
            _liteDb = liteDbContext;
            _selectedSteamIdentityService = teamSelectedSteamIdentityService;
        }
        
        
        public async Task<Team[]> FindAllTeamsWhereTheUserHasRights(ClaimsPrincipal cp, LiteDbUser user)
        {
            
            var steamIdentities = await _selectedSteamIdentityService.FindAllFrom(user.Id);
            var allTeams = steamIdentities.Where(x=>x.Team!=null).Select(x=>x.Team).Distinct().ToArray();

            foreach (var team in allTeams)
            {
                team.TeamSelectedSteamIdentities = (await _selectedSteamIdentityService.FindAllFrom(team.Id)).ToList();
            }
            if (cp.IsInRole("Admin")||cp.IsInRole("Mod")||cp.IsInRole("Captain"))
            {
                var tmpTeams = (await FindAll()).ToArray();
                foreach (var tmpTeam in tmpTeams)
                {
                    tmpTeam.TeamRole = "Admin";
                }
                return tmpTeams.ToArray();
            }
            //Team Admins and Mods get added as mods to the server. So they don't need to get handled here.
            var tmpServer = new List<Team>();
            
            var ownSteamIdentityTeamSelectedSteamIdentity = steamIdentities.FirstOrDefault(x=>x.SteamIdentity.LiteDbUser.Id== user.Id);
            if (ownSteamIdentityTeamSelectedSteamIdentity == null)
            {
                return tmpServer.ToArray();
            }

            var ownSteamIdentity = ownSteamIdentityTeamSelectedSteamIdentity.SteamIdentity;

            foreach (var singleTeam in allTeams)
            {
                singleTeam.TeamRole = singleTeam.TeamSelectedSteamIdentities?
                    .FirstOrDefault(x => x.SteamIdentity!=null && x.SteamIdentity.Id == ownSteamIdentity.Id)?.RoleOverwrite;
            }
            
            return allTeams.Where(x=>x.TeamRole=="Admin"||x.TeamRole=="Captain"||x.TeamRole=="Mod").ToArray();
            
        }

        public async Task<IEnumerable<Team>> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team")
                .FindAllAsync()).OrderByDescending(x => x.Id);
        }

        public async Task<Team> FindOne(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team")
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<Team> FindOne(string name)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team")
                .FindOneAsync(x => x.Name == name);
        }

        public async Task<bool> Upsert(Team team)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team")
                .UpsertAsync(team);
        }

        public async Task<bool> Delete(int id)
        {
            await _selectedSteamIdentityService.DeleteAllFromTeam(id);
            return await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team").DeleteAsync(id);
        }
    }
}