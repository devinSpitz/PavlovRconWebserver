using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public TeamService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<bool> CheckRightsTeamCaptainOrCaptain(int teamId,
            TeamSelectedSteamIdentityService teamSelectedSteamIdentityService, UserService userService,
            ClaimsPrincipal cp, SteamIdentity steamIdentity = null)
        {
            if (cp == null) return false;
            if (steamIdentity == null) return await RightsHandler.IsUserAtLeastInRole("Captain", cp, userService);
            var steamIdentityOnTeam = new TeamSelectedSteamIdentity();
            if (teamId == 0)
                steamIdentityOnTeam = await teamSelectedSteamIdentityService.FindOne(steamIdentity.Id);
            else
                steamIdentityOnTeam = await teamSelectedSteamIdentityService.FindOne(teamId, steamIdentity.Id);
            if (await RightsHandler.IsUserAtLeastInRole("Captain", cp, userService)) return true;
            if (steamIdentityOnTeam == null) return false;
            return RightsHandler.IsUserAtLeastInTeamRole("Captain", steamIdentityOnTeam.RoleOverwrite);
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
            return await _liteDb.LiteDatabaseAsync.GetCollection<Team>("Team").DeleteAsync(id);
        }
    }
}