using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class SteamIdentityService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;


        public SteamIdentityService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<SteamIdentity[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
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

        public async Task<SteamIdentity[]> FindAList(List<string> identities)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .FindAllAsync()).Where(x => identities.Contains(x.Id)).ToArray();
        }

        public async Task<bool> Upsert(SteamIdentity steamIdentity)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .UpsertAsync(steamIdentity);
        }
        
        public async Task<string> Insert(SteamIdentity steamIdentity)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity")
                .InsertAsync(steamIdentity);
        }

        public async Task<bool> Delete(string id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<SteamIdentity>("SteamIdentity").DeleteAsync(id);
        }
    }
}