using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MapsService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public MapsService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<Map>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .FindAll().OrderByDescending(x => x.Id);
        }

        public async Task<Map> FindOne(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<bool> Upsert(Map map)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .Upsert(map);
        }

        public async Task<bool> Delete(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map").Delete(id);
        }
    }
}