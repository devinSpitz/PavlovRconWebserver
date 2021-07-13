using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
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
            return _liteDb.LiteDatabase.GetCollection<ServerBans>("ServerBans")
                .FindAll().OrderByDescending(x => x.Id);
        }

        public async Task<ServerBans> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerBans>("ServerBans")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<List<ServerBans>> FindAllFromPavlovServerId(int pavlovServerId, bool getActive)
        {
            var result = _liteDb.LiteDatabase.GetCollection<ServerBans>("ServerBans")
                .Include(x => x.PavlovServer)
                .Find(x => x.PavlovServer.Id == pavlovServerId).ToList();

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
            return _liteDb.LiteDatabase.GetCollection<ServerBans>("ServerBans")
                .Upsert(serverBan);
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerBans>("ServerBans").Delete(id);
        }
    }
}