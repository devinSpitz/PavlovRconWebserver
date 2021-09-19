using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerBansService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public ServerBansService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<ServerBans>> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .FindAllAsync()).OrderByDescending(x => x.Id);
        }

        public async Task<ServerBans> FindOne(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<List<ServerBans>> FindAllFromPavlovServerId(int pavlovServerId, bool getActive)
        {
            var result = (await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == pavlovServerId)).ToList();

            if (getActive)
                result = result.Where(x =>
                {
                    try
                    {
                        return DateTime.Now < x.BannedDateTime.Add(x.BanSpan);
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                }).ToList();

            return result;
        }

        public async Task<bool> Upsert(ServerBans serverBan)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .UpsertAsync(serverBan);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans").DeleteAsync(id);
        }
    }
}