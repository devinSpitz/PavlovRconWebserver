using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MapsService
    {
        private ILiteDbIdentityContext _liteDb;
        
        
        public MapsService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public IEnumerable<Map> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .FindAll().OrderByDescending(x=>x.Id);
        }

        public Map FindOne(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public bool Upsert(Map map)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map")
                .Upsert(map);
        }

        public bool Delete(string id)
        {
            return _liteDb.LiteDatabase.GetCollection<Map>("Map").Delete(id);
        }
    }
}