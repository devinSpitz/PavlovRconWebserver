using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace PavlovRconWebserver.Services
{
    public class UserService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;
        private readonly RoleManager<LiteDbRole> _roleManager;
        private readonly UserManager<LiteDbUser> _userManager;

        public UserService(ILiteDbIdentityAsyncContext liteDbContext, UserManager<LiteDbUser> userMrg,
            RoleManager<LiteDbRole> roleMgr,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _userManager = userMrg;
            _roleManager = roleMgr;
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<LiteDbUser>> FindAllInRole(string roleName)
        {
            var liteDbUsers = new List<LiteDbUser>();
            foreach (var user in await FindAll())
                if (await _userManager.IsInRoleAsync(user, roleName))
                    liteDbUsers.Add(user);

            return liteDbUsers;
        }

        public void UserInRole(LiteDbUser user, ServerSelectedModsService serverSelectedModsService,
            PavlovServerService pavlovServerService, out bool isAdmin, out bool isCaptain, out bool isMod,
            out bool isOnPremise, out bool isServerRent, out bool isModSomeWhere, out bool Premium)
        {
            isAdmin = false;
            isCaptain = false;
            isMod = false;
            isOnPremise = false;
            isServerRent = false;
            isModSomeWhere = false;
            Premium = false;
            if (user == null) return;
            isAdmin = _userManager.IsInRoleAsync(user, "Admin").GetAwaiter().GetResult();
            isCaptain = _userManager.IsInRoleAsync(user, "Captain").GetAwaiter().GetResult();
            isMod = _userManager.IsInRoleAsync(user, "Mod").GetAwaiter().GetResult();
            isOnPremise = _userManager.IsInRoleAsync(user, "OnPremise").GetAwaiter().GetResult();
            isServerRent = _userManager.IsInRoleAsync(user, "ServerRent").GetAwaiter().GetResult();
            Premium = _userManager.IsInRoleAsync(user, "Premium").GetAwaiter().GetResult();
            isModSomeWhere = pavlovServerService.IsModSomeWhere(user, serverSelectedModsService).GetAwaiter()
                .GetResult();
        }

        public async Task<LiteDbUser[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<LiteDbUser>("LiteDbUser")
                .FindAllAsync()).ToArray();
        }

        public async Task<bool> Delete(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<LiteDbUser>("LiteDbUser")
                .DeleteAsync(new ObjectId(id));
        }

        public async Task<bool> IsUserInRole(string role, ClaimsPrincipal principal)
        {
            return await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(principal), role);
        }

        public async Task<bool> IsUserInRole(string role, LiteDbUser user)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<LiteDbUser> getUserFromCp(ClaimsPrincipal principal)
        {
            return await _userManager.GetUserAsync(principal);
        }

        public async Task CreateDefaultRoles()
        {
            // for updaters add roles which are should be there
            if (_roleManager.Roles?.FirstOrDefault(x => x.Name == "Mod") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Mod"
                });
            if (_roleManager.Roles?.FirstOrDefault(x => x.Name == "Captain") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Captain"
                });

            if (_roleManager.Roles?.FirstOrDefault(x => x.Name == "OnPremise") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "OnPremise"
                });

            if (_roleManager.Roles?.FirstOrDefault(x => x.Name == "ServerRent") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "ServerRent"
                });
            if (_roleManager.Roles?.FirstOrDefault(x => x.Name == "Premium") == null)
                await _roleManager.CreateAsync(new LiteDbRole
                {
                    Name = "Premium"
                });
        }
    }
}