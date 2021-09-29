using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MapsService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;

        public MapsService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<Map[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<Map>("Map")
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

        public async Task<Map> FindOne(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<Map>("Map")
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<bool> Upsert(Map map)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<Map>("Map")
                .UpsertAsync(map));
        }

        public async Task<bool> Delete(string id)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<Map>("Map").DeleteAsync(id));
        }
    }
}