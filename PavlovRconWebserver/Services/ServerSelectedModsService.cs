using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class ServerSelectedModsService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserService _userService;

        public ServerSelectedModsService(ILiteDbIdentityAsyncContext liteDbContext,
            SteamIdentityService steamIdentityService, UserService userService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
            _steamIdentityService = steamIdentityService;
            _userService = userService;
        }

        public async Task<bool> SaveModListToFileAndDb(List<string> userIds, PavlovServer server)
        {
            var steamIdentities = (await _steamIdentityService.FindAll()).ToList();
            //delete old stuff
            foreach (var old in await FindAllFrom(server)) await Delete(old.Id);
            //only save SteamIdsList to DB

            var steamIdentitiesToReturn = await SteamIdentitiesToReturn(userIds, server, steamIdentities);

            //Find all mods and add it to the list steamIdentitiesToReturn
            var additionalUsers = new List<LiteDbUser>();
            if (server.Owner == null || server.SshServer.Owner == null)
            {
                additionalUsers = (await _userService.FindAllInRole("Admin")).ToList(); // admins
                additionalUsers.AddRange(await _userService.FindAllInRole("Mod")); // mods
            }

            steamIdentitiesToReturn.AddRange(await SteamIdentitiesToReturn(
                additionalUsers.Select(x => x.Id.ToString()).ToList(), server, steamIdentities, false));

            await SaveToFile(server, steamIdentitiesToReturn);
            return true;
        }

        public async Task<List<string>> SteamIdentitiesToReturn(List<string> userdIds, PavlovServer server,
            List<SteamIdentity> steamIdentities, bool withInsert = true)
        {
            var steamIdentitiesToReturn = new List<string>();
            foreach (var newId in userdIds)
            {
                var steamIdentity = steamIdentities.FirstOrDefault(x => x.LiteDbUser?.Id == new ObjectId(newId));
                if (steamIdentity != null)
                {
                    var entry = new ServerSelectedMods
                    {
                        PavlovServer = server,
                        LiteDbUser = steamIdentity.LiteDbUser
                    };
                    steamIdentitiesToReturn.Add(steamIdentity.Id);
                    if (withInsert)
                        await Insert(entry);
                }
            }

            return steamIdentitiesToReturn;
        }


        private async Task<bool> SaveToFile(PavlovServer pavlovServer, List<string> steamIds)
        {
            var lines = steamIds.Select(steamIdentity => steamIdentity).ToList();
            RconStatic.WriteFile(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.ModList, lines.ToArray(),
                _notifyService);
            return true;
        }

        public async Task<ServerSelectedMods[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .Include(x => x.LiteDbUser)
                .Include(x => x.PavlovServer)
                .FindAllAsync()).ToArray();
        }

        public async Task<ServerSelectedMods[]> FindAllFrom(PavlovServer pavlovServer)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .Include(x => x.LiteDbUser)
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.PavlovServer.Id == pavlovServer.Id)).ToArray();
        }

        public async Task<ServerSelectedMods[]> FindAllFrom(LiteDbUser liteDbUser)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .Include(x => x.LiteDbUser)
                .Include(x => x.PavlovServer)
                .FindAsync(x => x.LiteDbUser.Id == liteDbUser.Id)).ToArray();
        }

        public async Task<int> Insert(ServerSelectedMods serverSelectedMap)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .InsertAsync(serverSelectedMap);
        }

        public async Task<int> DeleteFromServer(PavlovServer server)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .DeleteManyAsync(x => x.PavlovServer == server);
        }

        public async Task<bool> Delete(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ServerSelectedMods>("ServerSelectedMods")
                .DeleteAsync(id);
        }
    }
}