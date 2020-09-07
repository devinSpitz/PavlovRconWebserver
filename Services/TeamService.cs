using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamService
    {
        
        private ILiteDbIdentityContext _liteDb;
        
        
        public TeamService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<Team>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .FindAll().OrderByDescending(x=>x.Id);
        }

        public async Task<Team> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Find(x => x.Id == id).FirstOrDefault();
        }
        
        public async Task<Team> FindOne(string name)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Find(x => x.Name == name).FirstOrDefault();
        }

        public async Task<bool> Upsert(Team team)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team")
                .Upsert(team);
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<Team>("Team").Delete(id);
        }
    }
}