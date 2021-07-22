using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerService
    {
        private readonly ILiteDbIdentityContext _liteDb;


        public PavlovServerService(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }


        public async Task<bool> IsModSomeWhere(LiteDbUser user, ServerSelectedModsService serverSelectedModsService)
        {
            var servers = (await FindAll()).Where(x => x.ServerServiceState == ServerServiceState.active).ToList();
            var isModSomeWhere = false;
            foreach (var pavlovServer in servers)
                if (isModSomeWhere ||
                    await RightsHandler.IsModOnTheServer(serverSelectedModsService, pavlovServer, user.Id))
                    isModSomeWhere = true;

            return isModSomeWhere;
        }

        public async Task<IEnumerable<PavlovServer>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .FindAll().OrderByDescending(x => x.Id);
        }

        public List<PavlovServer> FindAllFrom(int sshServerId)
        {
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .Find(x => x.SshServer.Id == sshServerId).ToList();
        }

        public async Task<PavlovServer> FindOne(int id)
        {
            var pavlovServer = _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Find(x => x.Id == id).FirstOrDefault();
            return pavlovServer;
        }

        public async Task<KeyValuePair<PavlovServerViewModel, string>> CreatePavlovServer(PavlovServerViewModel server,
            RconService rconService, ServerSelectedMapService serverSelectedMapService,
            SshServerSerivce sshServerSerivce,
            PavlovServerService pavlovServerService)
        {
            //Todo stop when folder exist or when service already exist or when already a server with the same port is registerd on this ssh server
            string result = null;
            try
            {
                result += await rconService.UpdateInstallPavlovServer(server);
                result += "\n *******************************Update/Install Done*******************************";
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
                server.SshServer.SshKeyFileName = server.SshKeyFileNameRoot;
                server.SshServer.NotRootSshUsername = oldSSHcrid.SshUsername;
                result += await rconService.InstallPavlovServerService(server);
                server.SshServer.SshPassphrase = oldSSHcrid.SshPassphrase;
                server.SshServer.SshUsername = oldSSHcrid.SshUsername;
                server.SshServer.SshPassword = oldSSHcrid.SshPassword;
                server.SshServer.SshKeyFileName = oldSSHcrid.SshKeyFileName;

                //start server and stop server to get Saved folder etc.
                try
                {
                    await rconService.SystemDStart(server);
                    try
                    {
                        await SystemdService.UpdateAllServiceStates(rconService, pavlovServerService, sshServerSerivce);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }

                try
                {
                    await rconService.SystemDStop(server);
                }
                catch (Exception)
                {
                    //ignore
                }

                result +=
                    "\n *******************************Update/Install PavlovServerService Done*******************************";

                var pavlovServerGameIni = new PavlovServerGameIni();
                var selectedMaps = await serverSelectedMapService.FindAllFrom(server);
                await pavlovServerGameIni.SaveToFile(server, selectedMaps.ToList(), rconService);
                result += "\n *******************************Save server settings Done*******************************";
                //also create rcon settings
                var rconSettingsTempalte = "Password=" + server.TelnetPassword + "\nPort=" + server.TelnetPort;
                await rconService.WriteFile(server, server.ServerFolderPath + FilePaths.RconSettings,
                    rconSettingsTempalte);


                result += "\n *******************************create rconSettings Done*******************************";

                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                return new KeyValuePair<PavlovServerViewModel, string>(server, result + "\n " +
                                                                               "**********************************************Exception:***********************\n" +
                                                                               e.Message);
            }

            return new KeyValuePair<PavlovServerViewModel, string>(server, null);
        }


        // public async Task<KeyValuePair<PavlovServerViewModel,string>> RemovePavlovServerFromDisk(PavlovServerViewModel server,
        //   RconService rconService,ServerSelectedMapService serverSelectedMapService,SshServerSerivce sshServerSerivce,
        //   PavlovServerService pavlovServerService)
        // {
        //
        //     //Todo delete server if somethings goes wrong and also give it as an option may need root as well
        //     string result = null;
        //     try
        //     {
        //         result += await rconService.RemovePavlovServerFolder(server);
        //         result += "\n *******************************Update/Install Done*******************************";
        //         var oldSSHcrid = new SshServer()
        //         {
        //             SshPassphrase = server.SshServer.SshPassphrase,
        //             SshUsername = server.SshServer.SshUsername,
        //             SshPassword = server.SshServer.SshPassword,
        //             SshKeyFileName = server.SshServer.SshKeyFileName
        //         };
        //         server.SshServer.SshPassphrase = server.SshPassphraseRoot;
        //         server.SshServer.SshUsername = server.SshUsernameRoot;
        //         server.SshServer.SshPassword = server.SshPasswordRoot;
        //         server.SshServer.SshKeyFileName = server.SshKeyFileNameRoot;
        //         server.SshServer.NotRootSshUsername = oldSSHcrid.SshUsername;
        //         result += await rconService.InstallPavlovServerService(server);
        //         server.SshServer.SshPassphrase = oldSSHcrid.SshPassphrase;
        //         server.SshServer.SshUsername = oldSSHcrid.SshUsername;
        //         server.SshServer.SshPassword = oldSSHcrid.SshPassword;
        //         server.SshServer.SshKeyFileName = oldSSHcrid.SshKeyFileName;
        //
        //         //start server and stop server to get Saved folder etc.
        //         try
        //         {
        //             await rconService.SystemDStart(server);
        //         }
        //         catch (Exception)
        //         {
        //             //ignore
        //         }
        //
        //         try
        //         {
        //             await rconService.SystemDStart(server);
        //         }
        //         catch (Exception)
        //         {
        //             //ignore
        //         }
        //
        //         result +=
        //             "\n *******************************Update/Install PavlovServerService Done*******************************";
        //
        //         var pavlovServerGameIni = new PavlovServerGameIni()
        //         {
        //         };
        //         var selectedMaps = await serverSelectedMapService.FindAllFrom(server);
        //         await pavlovServerGameIni.SaveToFile(server, selectedMaps.ToList(), rconService);
        //         result += "\n *******************************Save server settings Done*******************************";
        //         //also create rcon settings
        //         var rconSettingsTempalte = "Password=" + server.TelnetPassword + "\nPort=" + server.TelnetPort;
        //         await rconService.WriteFile(server, server.ServerFolderPath + FilePaths.RconSettings, rconSettingsTempalte);
        //
        //
        //         result += "\n *******************************create rconSettings Done*******************************";
        //
        //         Console.WriteLine(result);
        //     }
        //     catch (Exception e)
        //     {
        //         return new KeyValuePair<PavlovServerViewModel, string>(server,result+"\n " +
        //                                                                       "**********************************************Exception:***********************\n" +
        //                                                                       e.Message);
        //     }
        //     return new KeyValuePair<PavlovServerViewModel, string>(server,null);
        // }
        //

        public async Task<bool> Upsert(PavlovServer pavlovServer, RconService service,
            SshServerSerivce sshServerSerivce, bool withCheck = true)
        {
            if (withCheck)
                pavlovServer = await sshServerSerivce.validatePavlovServer(pavlovServer, service);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer")
                .Upsert(pavlovServer);
        }

        public async Task<bool> Delete(int id,
            ServerSelectedWhitelistService serverSelectedWhiteList,
            ServerSelectedMapService serverSelectedMapService,
            ServerSelectedModsService serverSelectedModsService)
        {
            var server = await FindOne(id);
            await serverSelectedMapService.DeleteFromServer(server);
            await serverSelectedModsService.DeleteFromServer(server);
            await serverSelectedWhiteList.DeleteFromServer(server);
            return _liteDb.LiteDatabase.GetCollection<PavlovServer>("PavlovServer").Delete(id);
        }
    }
}