using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;
using Serilog.Events;

namespace PavlovRconWebserver.Services
{
    public class SshServerSerivce
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly IToastifyService _notifyService;
        private readonly PavlovServerService _pavlovServerService;

        public SshServerSerivce(ILiteDbIdentityAsyncContext liteDbContext,
            PavlovServerService pavlovServerServiceService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
            _pavlovServerService = pavlovServerServiceService;
        }

        public async Task<SshServer[]> FindAll()
        {
            var list = (await _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer")
                .Include(x=>x.Owner)
                .FindAllAsync()).ToArray();

            foreach (var single in list)
                single.PavlovServers = (await _pavlovServerService.FindAllFrom(single.Id)).ToList();

            return list;
        }

        public async Task<SshServer[]> FindAllWithRightsCheck(ClaimsPrincipal principal, LiteDbUser user)
        {
            var query = _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer")
                .Include(x=>x.Owner).Query();
            var list = new List<SshServer>();
            var rental = principal.IsInRole("ServerRent");
            if (principal.IsInRole("Admin"))
            {
                list.AddRange(await query.Where(x => true).ToListAsync());
            }
            else
            {
                if (principal.IsInRole("OnPremise"))
                    list.AddRange(await query.Where(x => x.Owner!=null &&  x.Owner.Id == user.Id).ToListAsync());
            }
            //var list = (
            //    .FindAllAsync()).ToArray();

            foreach (var single in list)
                    single.PavlovServers = (await _pavlovServerService.FindAllFrom(single.Id)).ToList();

            if (rental)
            {
                var all = await FindAll();
                foreach (var single in all)
                    single.PavlovServers = (await _pavlovServerService.FindAllFrom(single.Id))
                .Where(x => x.Owner !=null && x.Owner.Id == user.Id).ToList();

                return all.Where(x => x.PavlovServers.Any()).ToArray();

            }

            return list.ToArray();
        }


        public async Task<SshServer> FindOne(int id)
        {
            var single = await _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer")
                .Include(x=>x.Owner)
                .FindOneAsync(x => x.Id == id);

            single.PavlovServers = (await _pavlovServerService.FindAllFrom(single.Id)).ToList();

            return single;
        }

        public async Task<int> Insert(SshServer sshServer)
        {
            ValidateSshServer(sshServer);
            return await _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer")
                .InsertAsync(sshServer);
        }

        private static void ValidateSshServer(SshServer server)
        {
            if (server.SshPort <= 0) throw new SaveServerException("SshPort", "You need a SSH port!");

            if (string.IsNullOrEmpty(server.SshUsername))
                throw new SaveServerException("SshUsername", "You need a username!");

            if (string.IsNullOrEmpty(server.SshPassword) && (server.SshKeyFileName==null||!server.SshKeyFileName.Any()))
                throw new SaveServerException("SshPassword", "You need at least a password or a key file!");
        }

        public async Task<KeyValuePair<PavlovServerViewModel, string>> RemovePavlovServerFromDisk(
            PavlovServerViewModel server)
        {
            
            //Todo AuthError??
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start remove server!", LogEventLevel.Verbose,
                _notifyService);
            string result = null;
            try
            {
                //start server and stop server to get Saved folder etc.
                try
                {
                    await RconStatic.SystemDStop(server, _pavlovServerService);
                }
                catch (Exception)
                {
                    //ignore
                }

                
                result += await RconStatic.RemovePath(server, server.ServerFolderPath, _pavlovServerService);
                result += "\n *******************************delete folder Done*******************************";

                
                
                server.SshServer = await FindOne(server.sshServerId);
                if (server.SshServer == null) throw new CommandException("Could not get the sshServer!");
                var oldSSHcrid = new SshServer
                {
                    SshPassphrase = server.SshServer.SshPassphrase,
                    SshUsername = server.SshServer.SshUsername,
                    SshPassword = server.SshServer.SshPassword,
                    SshKeyFileName = server.SshServer.SshKeyFileName
                };
                server.SshServer.SshPassphrase = server.SshPassphraseRoot;
                server.SshServer.SshUsername = server.SshUsernameRoot;
                server.SshServer.SshPassword = server.SshPasswordRoot;
                if (server.SshKeyFileNameForm != null)
                {
                    await using var ms = new MemoryStream();
                    await server.SshKeyFileNameForm.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    server.SshKeyFileNameRoot = fileBytes;
                    // act on the Base64 data
                }
                server.SshServer.SshKeyFileName = server.SshKeyFileNameRoot;
                server.SshServer.NotRootSshUsername = oldSSHcrid.SshUsername;


                result += await RconStatic.RemovePath(server,
                    "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service", _pavlovServerService);

                //Remove the server from the sudoers file
                var sudoersPathParent = "/etc/sudoers.d";
                var sudoersPath = sudoersPathParent + "/pavlovRconWebserverManagement";
                if (RconStatic.RemoveServerLineToSudoersFile(server, _notifyService, sudoersPath, _pavlovServerService))
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("server line removed from sudoers file!",
                        LogEventLevel.Verbose, _notifyService);
                }
                else
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "Could not remove the server line from sudoers file!", LogEventLevel.Fatal, _notifyService);

                    return new KeyValuePair<PavlovServerViewModel, string>(server,
                        result + "Could not remove the server line from sudoers file!");
                }
                
                //Handle the presets an newer systemd
                RconStatic.AddLineToNewerSystemDsIfNeeded(server,_notifyService,true);



                DataBaseLogger.LogToDatabaseAndResultPlusNotify(result, LogEventLevel.Verbose, _notifyService);
            }
            catch (Exception e)
            {
                return new KeyValuePair<PavlovServerViewModel, string>(server, result + "\n " +
                                                                               "**********************************************Exception:***********************\n" +
                                                                               e.Message);
            }

            return new KeyValuePair<PavlovServerViewModel, string>(server, null);
        }


        public async Task<bool> Update(SshServer sshServer)
        {
            SshServer old = null;
            if (string.IsNullOrEmpty(sshServer.SshPassphrase) || string.IsNullOrEmpty(sshServer.SshPassword))
            {
                old = await FindOne(sshServer.Id);
                if (old != null)
                {
                    if (string.IsNullOrEmpty(sshServer.SshPassphrase)) sshServer.SshPassphrase = old.SshPassphrase;
                    if (string.IsNullOrEmpty(sshServer.SshPassword)) sshServer.SshPassword = old.SshPassword;
                }
            }

            ValidateSshServer(sshServer);

            return await _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer")
                .UpdateAsync(sshServer);
        }

        public async Task<bool> Delete(int id)
        {
            var pavlovServers = (await _pavlovServerService.FindAll()).Where(x => x.SshServer.Id == id);
            foreach (var pavlovServer in pavlovServers)
                await _pavlovServerService.Delete(pavlovServer.Id);
            return await _liteDb.LiteDatabaseAsync.GetCollection<SshServer>("SshServer").DeleteAsync(id);
        }
    }
}