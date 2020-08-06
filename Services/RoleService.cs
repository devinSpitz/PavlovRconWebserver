using AspNetCore.Identity.LiteDB.Data;
using LiteDB;

namespace PavlovRconWebserver.Services
{
    public class RoleService
    {
        private LiteDatabase _liteDb;

        public RoleService(ILiteDbContext liteDbContext)
        {
            _liteDb = liteDbContext.LiteDatabase;
        }
        
    }
}