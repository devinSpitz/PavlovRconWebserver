
using LiteDB;
using LiteDB.Identity.Database;

namespace PavlovRconWebserver.Services
{
    public class RoleService
    {
        private ILiteDbIdentityContext _liteDb;

        public RoleService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }
        
    }
}