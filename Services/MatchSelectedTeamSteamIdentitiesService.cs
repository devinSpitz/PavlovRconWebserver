using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class MatchSelectedTeamSteamIdentitiesService
    {
        private readonly ILiteDbIdentityContext _liteDb;

        public MatchSelectedTeamSteamIdentitiesService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<MatchTeamSelectedSteamIdentity>> FindAll()
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .FindAllAsync();
        }

        public async Task<IEnumerable<MatchTeamSelectedSteamIdentity>> FindAllSelectedForMatchAndTeam(int matchId,
            int teamId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .FindAllAsync()).Where(x => x.matchId == matchId && x.TeamId == teamId);
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
            var tmp = (await _liteDb.LiteDatabaseAsync
                .GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity").FindAllAsync()).ToList();


            return await _liteDb.LiteDatabaseAsync.GetCollection<MatchTeamSelectedSteamIdentity>("MatchTeamSelectedSteamIdentity")
                .DeleteManyAsync(x => x.matchId == matchId);
        }
    }
}