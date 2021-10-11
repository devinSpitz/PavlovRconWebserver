using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerBansService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;


        public ServerBansService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<ServerBans[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

        public async Task<ServerBans> FindOne(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<ServerBans[]> FindAllFromPavlovServerId(int pavlovServerId, bool getActive)
        {
            var result = (await _liteDb.LiteDatabaseAsync.GetCollection<ServerBans>("ServerBans")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == pavlovServerId)).ToArray();

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
                }).ToArray();

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