using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class UserService
    {
        private ILiteDbIdentityContext _liteDb;
        private UserManager<LiteDB.Identity.Models.LiteDbUser> _userManager;
        public UserService(ILiteDbIdentityContext liteDbContext,UserManager<LiteDB.Identity.Models.LiteDbUser> userMrg)
        {
            _userManager = userMrg;
            _liteDb = liteDbContext;
        }

        public IEnumerable<LiteDbUser> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<LiteDbUser>("LiteDbUser")
                .FindAll();
        }

        public bool Delete(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<LiteDbUser>("LiteDbUser").Delete(id);
        }
        
        
        public async Task<bool> IsUserNotInRole(string role,ClaimsPrincipal principal)
        {
            return (!await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(principal)),role)); 
        }
        
        public async Task<bool> IsUserInRole(string role,ClaimsPrincipal principal)
        {
            return (await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(principal)),role)); 
        }
    }
}