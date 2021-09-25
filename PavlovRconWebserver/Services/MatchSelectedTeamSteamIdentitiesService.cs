using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedTeamSteamIdentitiesService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;

        public MatchSelectedTeamSteamIdentitiesService(ILiteDbIdentityAsyncContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<MatchTeamSelectedSteamIdentity[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .FindAllAsync()).ToArray();
        }

        public async Task<MatchTeamSelectedSteamIdentity[]> FindAllSelectedForMatchAndTeam(int matchId,
            int teamId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .FindAllAsync()).Where(x => x.matchId == matchId && x.TeamId == teamId).ToArray();
        }

        public async Task<int> Upsert(List<MatchTeamSelectedSteamIdentity> matchTeamSelectedSteamIdentity, int matchId,
            int teamId)
        {
            if (matchId != 0)
                await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                    .DeleteManyAsync(x => x.matchId == matchId && x.TeamId == teamId);

            return await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .Include(x => x.matchId == matchId && x.TeamId == teamId)
                .UpsertAsync(matchTeamSelectedSteamIdentity);
        }


        public async Task<int> RemoveFromMatch(int matchId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .DeleteManyAsync(x => x.matchId == matchId);
        }
    }
}