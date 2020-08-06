using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB.Data;
using AspNetCore.Identity.LiteDB.Models;
using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class UserService
    {
        private LiteDatabase _liteDb;
        private UserManager<InbuildUser> userManager;

        public UserService(ILiteDbContext liteDbContext,UserManager<InbuildUser> userMrg)
        {
            userManager = userMrg;
            _liteDb = liteDbContext.LiteDatabase;
        }

        public IEnumerable<InbuildUser> FindAll()
        {
            return _liteDb.GetCollection<InbuildUser>("Users")
                .FindAll();
        }

        public bool Delete(string id)
        {
            return _liteDb.GetCollection<InbuildUser>("Users").Delete(id);
        }
        
        
        public async Task<bool> IsUserInRole(string role,ClaimsPrincipal principal)
        {
            
            return (!await userManager.IsInRoleAsync((await userManager.GetUserAsync(principal)),role));
            
            
        }
    }
}