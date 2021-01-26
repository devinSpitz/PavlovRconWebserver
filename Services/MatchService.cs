using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchService
    {
        private ILiteDbIdentityContext _liteDb;
        
        
        public MatchService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<Match>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .FindAll().OrderByDescending(x=>x.Id);
        }

        public async Task<Match> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<bool> Upsert(Match match)
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match")
                .Upsert(match);
        }

        public async Task<bool> Delete(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<Match>("Match").Delete(id);
        }
    }
}