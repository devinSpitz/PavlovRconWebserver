using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class TeamSelectedSteamIdentityService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public TeamSelectedSteamIdentityService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }


        public async Task<TeamSelectedSteamIdentity[]> FindAllFrom(int teamId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAsync(x => x.Team.Id == teamId)).ToArray();
        }
        public async Task<TeamSelectedSteamIdentity[]> FindAllFrom(ObjectId userId)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .Include(x=>x.Team.TeamSelectedSteamIdentities)
                .FindAsync(x => x.SteamIdentity.LiteDbUser.Id == userId)).ToArray();
        }

        public async Task<TeamSelectedSteamIdentity[]> FindAllFrom(SteamIdentity steamIdentity)
        {
            return (await _liteDb.LiteDatabaseAsync
                .GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
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

        public async Task<bool> DeleteAllFromTeam(int teamId)
        {
            var teamSelectedSteamIdentities = (await _liteDb.LiteDatabaseAsync
                .GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity").Include(x => x.Team)
                .FindAllAsync()).ToArray();
            foreach (var teamSelectedSteamIdentity in teamSelectedSteamIdentities.Where(x=>x.Team.Id==teamId))
            {
                await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                    .DeleteAsync(teamSelectedSteamIdentity.Id);
            }
            return true;
        }
        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .DeleteAsync(id);
        }
    }
}