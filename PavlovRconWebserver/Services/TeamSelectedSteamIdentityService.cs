using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamSelectedSteamIdentityService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;

        public TeamSelectedSteamIdentityService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }
        

        public async Task<TeamSelectedSteamIdentity[]> FindAllFrom(int teamId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAsync(x => x.Team.Id == teamId)).ToArray();
        }

        public async Task<TeamSelectedSteamIdentity[]> FindAllFrom(SteamIdentity steamIdentity)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAsync(x => x.SteamIdentity.Id == steamIdentity.Id)).ToArray();
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(string steamIdentityId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindOneAsync(x => x.SteamIdentity.Id == steamIdentityId);
        }

        public async Task<TeamSelectedSteamIdentity> FindOne(int teamId, string steamIdentityId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindOneAsync(x => x.Team.Id == teamId && x.SteamIdentity.Id == steamIdentityId);
        }

        public async Task<int> Insert(TeamSelectedSteamIdentity teamSelectedSteamIdentity)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .InsertAsync(teamSelectedSteamIdentity);
        }

        public async Task<bool> Update(TeamSelectedSteamIdentity teamSelectedSteamIdentity)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .UpdateAsync(teamSelectedSteamIdentity);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .DeleteAsync(id);
        }
    }
}