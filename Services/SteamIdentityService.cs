using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class SteamIdentityService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public SteamIdentityService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<SteamIdentity>> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .FindAllAsync()).OrderByDescending(x => x.Id);
        }


        public async Task<SteamIdentity> FindOne(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<SteamIdentity> FindOne(ObjectId liteDbUserId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .FindOneAsync(x => x.LiteDbUser.Id == liteDbUserId);
        }

        public async Task<IEnumerable<SteamIdentity>> FindAList(List<string> identities)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .FindAllAsync()).Where(x => identities.Contains(x.Id));
        }

        public async Task<bool> Upsert(SteamIdentity steamIdentity)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .UpsertAsync(steamIdentity));
        }

        public async Task<bool> Delete(long id)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity").DeleteAsync(id));
        }
    }
}