using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerPlayerService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public PavlovServerPlayerService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<PavlovServerPlayer>> FindAllFromServer(int serverId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServerPlayer>("PavlovServerPlayer")
                .FindAllAsync()).Where(x => x.ServerId == serverId);
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