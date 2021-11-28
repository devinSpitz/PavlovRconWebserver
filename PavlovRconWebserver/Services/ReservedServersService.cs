using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;
using Serilog.Sinks.Models;

namespace PavlovRconWebserver.Services
{
    public class ReservedServersService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public ReservedServersService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<ReservedServer[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").FindAllAsync()).ToArray();
        }        
        
        public async Task<ReservedServer[]> FindByEmail(string email)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").FindAllAsync()).Where(x=>x.Email==email).ToArray();
        }        
        public async Task<bool> Remove(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").DeleteAsync(id);
        }    
        public async Task<bool> Add(ReservedServer reservedServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer")
                .UpsertAsync(reservedServer);
        }
    }
    
}