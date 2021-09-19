using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;

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