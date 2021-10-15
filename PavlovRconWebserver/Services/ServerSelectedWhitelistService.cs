using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedWhitelistService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;

        public ServerSelectedWhitelistService(ILiteDbIdentityAsyncContext liteDbContext,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<bool> SaveWhiteListToFileAndDb(List<string> steamIds, PavlovServer server)
        {
            //delete old stuff
            foreach (var old in await FindAllFrom(server)) await Delete(old.Id);
            //only save SteamIdsList to DB
            foreach (var newId in steamIds)
            {
                var entry = new ServerSelectedWhiteList
                {
                    PavlovServer = server,
                    SteamIdentityId = newId
                };
                await Insert(entry);
            }

            SaveToFile(server, steamIds);
            return true;
        }

        private bool SaveToFile(PavlovServer pavlovServer, List<string> steamIds)
        {
            var lines = steamIds.Select(steamIdentity => steamIdentity + ";").ToArray();
            var content = string.Join("\n", lines);
            RconStatic.WriteFile(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.WhiteList, content,
                _notifyService);
            return true;
        }

        public async Task<ServerSelectedWhiteList[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAllAsync()).ToArray();
        }

        public async Task<ServerSelectedWhiteList[]> FindAllFrom(PavlovServer sshServer)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == sshServer.Id)).ToArray();
        }

        public async Task<ServerSelectedWhiteList[]> FindAllFrom(string steamIdentityId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.SteamIdentityId == steamIdentityId)).ToArray();
        }

        public async Task<ServerSelectedWhiteList> FindOne(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .FindOneAsync(x => x.Id == id);
        }

        public async Task<ServerSelectedWhiteList> FindSelectedMap(int serverId, string steamIdentityId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindOneAsync(x => x.SteamIdentityId == steamIdentityId && x.PavlovServer.Id == serverId);
        }

        public async Task<int> Insert(ServerSelectedWhiteList serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .InsertAsync(serverSelectedMap);
        }

        public async Task<bool> Update(ServerSelectedWhiteList serverSelectedWhiteList)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .UpdateAsync(serverSelectedWhiteList);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .DeleteManyAsync(x => x.PavlovServer.Id == server.Id);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .DeleteAsync(id);
        }
    }
}