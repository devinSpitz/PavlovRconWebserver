using LiteDB.Identity.Async.Database;

namespace PavlovRconWebserver.Services
{
    public class RoleService
    {
        private ILiteDbIdentityAsyncContext _liteDb;

        public RoleService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }
    }
}