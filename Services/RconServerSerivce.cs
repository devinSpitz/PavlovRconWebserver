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

        public int Insert(RconServer rconServer)
        {
            return _liteDb.GetCollection<RconServer>("RconServer")
                .Insert(rconServer);
        }

        public bool Update(RconServer rconServer)
        {
            if (string.IsNullOrEmpty(rconServer.Password) || string.IsNullOrEmpty(rconServer.SshPassphrase) || string.IsNullOrEmpty(rconServer.SshPassword))
            {
                var old = FindOne(rconServer.Id);
                if (string.IsNullOrEmpty(rconServer.Password)) rconServer.Password = old.Password;
                if (string.IsNullOrEmpty(rconServer.SshPassphrase)) rconServer.SshPassphrase = old.SshPassphrase;
                if (string.IsNullOrEmpty(rconServer.SshPassword)) rconServer.SshPassword = old.SshPassword;
            }
            
               
            return _liteDb.GetCollection<RconServer>("RconServer")
                .Update(rconServer); 
            
           
        }

        public bool Delete(int id)
        {
            return _liteDb.GetCollection<RconServer>("RconServer").Delete(id);
        }
    }
}