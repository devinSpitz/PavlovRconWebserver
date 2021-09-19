using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Services
{
    public class UserService
    {
        private readonly ILiteDbIdentityContext _liteDb;
        private readonly RoleManager<LiteDbRole> _roleManager;
        private readonly UserManager<LiteDbUser> _userManager;

        public UserService(ILiteDbIdentityContext liteDbContext, UserManager<LiteDbUser> userMrg,
            RoleManager<LiteDbRole> roleMgr)
        {
            _userManager = userMrg;
            _roleManager = roleMgr;
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<LiteDbUser>> FindAllInRole(string roleId)
        {
            var liteDbUsers = new List<LiteDbUser>();
            foreach (var user in await FindAll())
                if (await _userManager.IsInRoleAsync(user, roleId))
                    liteDbUsers.Add(user);

            return liteDbUsers;
        }

        public async Task<IEnumerable<LiteDbUser>> FindAll()
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<LiteDbUser>("LiteDbUser")
                .FindAllAsync();
        }

        public async Task<bool> Delete(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<LiteDbUser>("LiteDbUser").DeleteAsync(new ObjectId(id));
        }


        public async Task<bool> IsUserNotInRole(string role, ClaimsPrincipal principal)
        {
            return !await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(principal), role);
        }

        public async Task<bool> IsUserInRole(string role, ClaimsPrincipal principal)
        {
            return await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(principal), role);
        }

        public async Task<LiteDbUser> getUserFromCp(ClaimsPrincipal principal)
        {
            return await _userManager.GetUserAsync(principal);
        }

        public async Task CreateDefaultRoles()
        {
            // for updaters add roles which are should be there
            if (_roleManager.Roles.ToList().FirstOrDefault(x => x.Name == "Mod") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Mod"
                });
            if (_roleManager.Roles.ToList().FirstOrDefault(x => x.Name == "Captain") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Captain"
                });
        }
    }
}