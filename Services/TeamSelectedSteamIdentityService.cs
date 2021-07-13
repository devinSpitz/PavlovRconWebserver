using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamSelectedSteamIdentityService
    {
        private readonly ILiteDbIdentityContext _liteDb;

        public TeamSelectedSteamIdentityService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<TeamSelectedSteamIdentity>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAll();
        }

        public async Task<IEnumerable<TeamSelectedSteamIdentity>> FindAllFrom(int teamId)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Find(x => x.Team.Id == teamId);
        }

        public async Task<IEnumerable<TeamSelectedSteamIdentity>> FindAllFrom(SteamIdentity steamIdentity)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Find(x => x.SteamIdentity.Id == steamIdentity.Id);
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(string steamIdentityId)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Find(x => x.SteamIdentity.Id == steamIdentityId).FirstOrDefault();
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(int teamId, string steamIdentityId)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Find(x => x.Team.Id == teamId && x.SteamIdentity.Id == steamIdentityId).FirstOrDefault();
        }

        public async Task<int> Insert(TeamSelectedSteamIdentity teamSelectedSteamIdentity)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Insert(teamSelectedSteamIdentity);
        }

        public async Task<int> Upsert(List<TeamSelectedSteamIdentity> teamSelectedSteamIdentities)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Upsert(teamSelectedSteamIdentities);
        }

        public async Task<bool> Update(TeamSelectedSteamIdentity teamSelectedSteamIdentity)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Update(teamSelectedSteamIdentity);
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Delete(id);
        }
    }
}