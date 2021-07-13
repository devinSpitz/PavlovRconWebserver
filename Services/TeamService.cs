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
            return await RightsHandler.IsUserAtLeastInTeamRole("Captain", steamIdentityOnTeam.RoleOverwrite);
        }

        public async Task<IEnumerable<Team>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .FindAll().OrderByDescending(x => x.Id);
        }

        public async Task<Team> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<Team> FindOne(string name)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Find(x => x.Name == name).FirstOrDefault();
        }

        public async Task<bool> Upsert(Team team)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Upsert(team);
        }

        public async Task<bool> Delete(int id)
        {
            //Todo remove all connected Data
            return _liteDb.LiteDatabase.GetCollection<Team>("Team").Delete(id);
        }
    }
}