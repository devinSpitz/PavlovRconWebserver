using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedWhitelistService
    {
        private readonly ILiteDbIdentityContext _liteDb;

        public ServerSelectedWhitelistService(ILiteDbIdentityContext liteDbContext)
        {
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
            var lines = steamIds.Select(steamIdentity => steamIdentity + ";").ToList();
            var content = string.Join(Environment.NewLine, lines);
            RconStatic.WriteFile(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.WhiteList, content);
            return true;
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAll()
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAllAsync();
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAllFrom(PavlovServer sshServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == sshServer.Id);
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAllFrom(string steamIdentityId)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.SteamIdentityId == steamIdentityId);
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

        private async Task<int> Insert(ServerSelectedWhiteList serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .InsertAsync(serverSelectedMap);
        }

        public async Task<bool> Update(ServerSelectedWhiteList serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .UpdateAsync(serverSelectedMap);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .DeleteManyAsync(x => x.PavlovServer == server);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList").DeleteAsync(id);
        }
    }
}