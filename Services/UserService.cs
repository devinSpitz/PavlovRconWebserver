using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace PavlovRconWebserver.Services
{
    public class UserService
    {
        private ILiteDbIdentityContext _liteDb;
        private UserManager<LiteDbUser> _userManager;
        private readonly RoleManager<LiteDbRole> _roleManager;
        public UserService(ILiteDbIdentityContext liteDbContext,UserManager<LiteDbUser> userMrg, RoleManager<LiteDbRole> roleMgr)
        {
            _userManager = userMrg;
            _roleManager = roleMgr;
            _liteDb = liteDbContext;
        }

        public IEnumerable<LiteDbUser> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<LiteDbUser>("LiteDbUser")
                .FindAll();
        }

        public bool Delete(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<LiteDbUser>("LiteDbUser").Delete(new ObjectId(id));
        }
        
        
        public async Task<bool> IsUserNotInRole(string role,ClaimsPrincipal principal)
        {
            return (!await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(principal)),role)); 
        }
        
        public async Task<bool> IsUserInRole(string role,ClaimsPrincipal principal)
        {
            return (await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(principal)),role)); 
        }

        public async Task CreateDefaultRoles()
        {
            // for updaters add roles which are should be there
            if (_roleManager.Roles.ToList().FirstOrDefault(x => x.Name == "Mod") == null)
            {
                await _roleManager.CreateAsync(new LiteDbRole()
                {
                    Name = "Mod"
                });
            }
            if (_roleManager.Roles.ToList().FirstOrDefault(x => x.Name == "Captain") == null)
            {
                await _roleManager.CreateAsync(new LiteDbRole()
                {
                    Name = "Captain"
                });
            }
        }
    }
}