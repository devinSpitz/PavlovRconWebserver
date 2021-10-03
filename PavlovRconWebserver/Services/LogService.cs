using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using Serilog.Sinks.Models;

namespace PavlovRconWebserver.Services
{
    public class LogService
    {
        private readonly IToastifyService _notifyService;
        private readonly ILiteDbIdentityAsyncContext _liteDb;

        public LogService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<LiteDbLog[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<LiteDbLog>("LiteDbLog").FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

    }
}