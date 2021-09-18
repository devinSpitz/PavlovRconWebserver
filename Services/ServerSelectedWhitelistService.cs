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

            await SaveToFile(server, steamIds);
            return true;
        }

        private async Task<bool> SaveToFile(PavlovServer pavlovServer, List<string> steamIds)
        {
            var lines = steamIds.Select(steamIdentity => steamIdentity + ";").ToList();
            var content = string.Join(Environment.NewLine, lines);
            RconStatic.WriteFile(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.WhiteList, content);
            return true;
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .FindAll();
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAllFrom(PavlovServer sshServer)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .Find(x => x.PavlovServer.Id == sshServer.Id);
        }

        public async Task<IEnumerable<ServerSelectedWhiteList>> FindAllFrom(string steamIdentityId)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .Find(x => x.SteamIdentityId == steamIdentityId);
        }

        public async Task<ServerSelectedWhiteList> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<ServerSelectedWhiteList> FindSelectedMap(int serverId, string steamIdentityId)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Include(x => x.PavlovServer)
                .Find(x => x.SteamIdentityId == steamIdentityId && x.PavlovServer.Id == serverId).FirstOrDefault();
        }

        private async Task<int> Insert(ServerSelectedWhiteList serverSelectedMap)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Insert(serverSelectedMap);
        }

        public async Task<bool> Update(ServerSelectedWhiteList serverSelectedMap)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .Update(serverSelectedMap);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList")
                .DeleteMany(x => x.PavlovServer == server);
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<ServerSelectedWhiteList>("ServerSelectedWhiteList").Delete(id);
        }
    }
}