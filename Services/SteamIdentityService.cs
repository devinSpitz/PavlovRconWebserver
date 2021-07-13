using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using LiteDB.Identity.Database;
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
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .FindAll().OrderByDescending(x => x.Id);
        }


        public async Task<SteamIdentity> FindOne(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<SteamIdentity> FindOne(ObjectId liteDbUserId)
        {
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity")
                .Include(x => x.LiteDbUser)
                .Find(x => x.LiteDbUser.Id == liteDbUserId).FirstOrDefault();
        }

        public async Task<IEnumerable<SteamIdentity>> FindAList(List<string> identities)
        {
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity")
                .FindAll().Where(x => identities.Contains(x.Id));
        }

        public async Task<bool> Upsert(SteamIdentity steamIdentity)
        {
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity")
                .Upsert(steamIdentity);
        }

        public async Task<bool> Delete(long id)
        {
            return _liteDb.LiteDatabase.GetCollection<SteamIdentity>("SteamIdentity").Delete(id);
        }
    }
}