using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
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
        

        public async Task<IEnumerable<TeamSelectedSteamIdentity>> FindAllFrom(int teamId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAsync(x => x.Team.Id == teamId));
        }

        public async Task<IEnumerable<TeamSelectedSteamIdentity>> FindAllFrom(SteamIdentity steamIdentity)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .Include(x => x.SteamIdentity)
                .Include(x => x.SteamIdentity.LiteDbUser)
                .Include(x => x.Team)
                .FindAsync(x => x.SteamIdentity.Id == steamIdentity.Id));
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

        public async Task<int> Upsert(List<TeamSelectedSteamIdentity> teamSelectedSteamIdentities)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<TeamSelectedSteamIdentity>("TeamSelectedSteamIdentity")
                .UpsertAsync(teamSelectedSteamIdentities);
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