using System.Collections.Generic;
using System.Linq;
using AspNetCore.Identity.LiteDB.Data;
using LiteDB;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class RconServerSerivce
    {
        private LiteDatabase _liteDb;

        public RconServerSerivce(ILiteDbContext liteDbContext)
        {
            _liteDb = liteDbContext.LiteDatabase;
        }

        public IEnumerable<RconServer> FindAll()
        {
            return _liteDb.GetCollection<RconServer>("RconServer")
                .FindAll();
        }

        public RconServer FindOne(int id)
        {
            return _liteDb.GetCollection<RconServer>("RconServer")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public int Insert(RconServer forecast)
        {
            return _liteDb.GetCollection<RconServer>("RconServer")
                .Insert(forecast);
        }

        public bool Update(RconServer forecast)
        {
            return _liteDb.GetCollection<RconServer>("RconServer")
                .Update(forecast);
        }

        public bool Delete(int id)
        {
            return _liteDb.GetCollection<RconServer>("RconServer").Delete(id);
        }
    }
}