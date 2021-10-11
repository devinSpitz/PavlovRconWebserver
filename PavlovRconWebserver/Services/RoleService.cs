using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;

namespace PavlovRconWebserver.Services
{
    public class RoleService
    {
        private readonly IToastifyService _notifyService;
        private ILiteDbIdentityAsyncContext _liteDb;

        public RoleService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }
    }
}