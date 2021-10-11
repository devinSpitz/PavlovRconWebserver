using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerPlayerService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;


        public PavlovServerPlayerService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<PavlovServerPlayer[]> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .FindAllAsync()).Where(x => x.ServerId == serverId).ToArray();
        }

        public async Task<int> Upsert(List<PavlovServerPlayer> pavlovServerPlayers, int serverId)
        {
            var deletedPlayers = await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .DeleteManyAsync(x => x.ServerId == serverId);
            var savedPlayers = await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .InsertAsync(pavlovServerPlayers);

            return savedPlayers;
        }
    }
}