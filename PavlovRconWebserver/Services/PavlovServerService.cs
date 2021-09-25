using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class PavlovServerService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly ServerSelectedWhitelistService _serverSelectedWhitelistService;


        public PavlovServerService(ILiteDbIdentityAsyncContext liteDbContext,
            ServerSelectedModsService serverSelectedModsService,
            ServerSelectedWhitelistService serverSelectedWhitelistService,
            ServerSelectedMapService serverSelectedMapService)
        {
            _liteDb = liteDbContext;
            _serverSelectedMapService = serverSelectedMapService;
            _serverSelectedModsService = serverSelectedModsService;
            _serverSelectedWhitelistService = serverSelectedWhitelistService;
        }


        public async Task<bool> IsModSomeWhere(LiteDbUser user, ServerSelectedModsService serverSelectedModsService)
        {
            var servers = (await FindAll()).ToArray();
            var isModSomeWhere = false;
            foreach (var pavlovServer in servers)
                if (isModSomeWhere ||
                    await RightsHandler.IsModOnTheServer(serverSelectedModsService, pavlovServer, user.Id))
                    isModSomeWhere = true;

            return isModSomeWhere;
        }

        public async Task<PavlovServer[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

        public async Task<PavlovServer[]> FindAllFrom(int sshServerId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer").Include(x => x.SshServer)
                .FindAsync(x => x.SshServer.Id == sshServerId)).ToArray();
        }

        public async Task<PavlovServer> FindOne(int id)
        {
            var pavlovServer = await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .FindOneAsync(x => x.Id == id);
            return pavlovServer;
        }


        public async Task<KeyValuePair<PavlovServerViewModel, string>> CreatePavlovServer(PavlovServerViewModel server)
        {
            string result = null;
            try
            {
                //Check stuff
                var exist = RconStatic.DoesPathExist(server, server.ServerFolderPath);
                if (exist == "true") throw new CommandExceptionCreateServerDuplicate("ServerFolderPath already exist!");

                exist = RconStatic.DoesPathExist(server,
                    "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service");
                if (exist == "true") throw new CommandExceptionCreateServerDuplicate("Systemd Service already exist!");

                var portsUsed = (await FindAll()).Where(x => x.SshServer.Id == server.SshServer.Id)
                    .FirstOrDefault(x => x.ServerPort == server.ServerPort || x.TelnetPort == server.TelnetPort);
                if (portsUsed != null)
                {
                    if (portsUsed.ServerPort == server.ServerPort)
                        throw new CommandExceptionCreateServerDuplicate("The server port is already used!");

                    if (portsUsed.TelnetPort == server.TelnetPort)
                        throw new CommandExceptionCreateServerDuplicate("The telnet port is already used!");
                }

                result += await RconStatic.UpdateInstallPavlovServer(server,this);
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
                try
                {
                    result += RconStatic.InstallPavlovServerService(server);
                }
                catch (CommandException)
                {
                    //If crash inside here the user login is still root. If the root login is bad this will fail to remove the server afterwards
                    OverwrideTheNormalSSHLoginData(server, oldSSHcrid);
                    throw;
                }

                OverwrideTheNormalSSHLoginData(server, oldSSHcrid);

                //start server and stop server to get Saved folder etc.
                try
                {
                    await RconStatic.SystemDStart(server,this);
                }
                catch (Exception)
                {
                    //ignore
                }

                try
                {
                    await RconStatic.SystemDStop(server,this);
                }
                catch (Exception)
                {
                    //ignore
                }

                result +=
                    "\n *******************************Update/Install PavlovServerService Done*******************************";

                var pavlovServerGameIni = new PavlovServerGameIni();
                var selectedMaps = await _serverSelectedMapService.FindAllFrom(server);
                pavlovServerGameIni.SaveToFile(server, selectedMaps);
                result += "\n *******************************Save server settings Done*******************************";
                //also create rcon settings
                var rconSettingsTempalte = "Password=" + server.TelnetPassword + "\nPort=" + server.TelnetPort;
                RconStatic.WriteFile(server, server.ServerFolderPath + FilePaths.RconSettings,
                    rconSettingsTempalte);


                result += "\n *******************************create rconSettings Done*******************************";

                Console.WriteLine(result);
            }
            catch (CommandExceptionCreateServerDuplicate e)
            {
                throw new CommandExceptionCreateServerDuplicate(e.Message);
            }
            catch (Exception e)
            {
                return new KeyValuePair<PavlovServerViewModel, string>(server, result + "\n " +
                                                                               "**********************************************Exception:***********************\n" +
                                                                               e.Message);
            }

            return new KeyValuePair<PavlovServerViewModel, string>(server, null);
        }


        public async Task CheckStateForAllServers()
        {
            var pavlovServers = await FindAll();

            foreach (var signleServer in pavlovServers)
                try
                {
                    await UpdateServerState(signleServer, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
        }

        private async Task UpdateServerState(PavlovServer signleServer, bool withCheck)
        {
            var serverWithState = await GetServerServiceState(signleServer);
            await Upsert(serverWithState, withCheck);
        }

        /// <summary>
        /// </summary>
        /// <param name="pavlovServer"></param>
        /// <param name="rconService"></param>
        /// <returns></returns>
        private async Task<PavlovServer> GetServerServiceState(PavlovServer pavlovServer)
        {
            var state = await RconStatic.SystemDCheckState(pavlovServer);
            // state can be: active, inactive,disabled
            if (state == "active")
                pavlovServer.ServerServiceState = ServerServiceState.active;
            else if (state == "inactive")
                pavlovServer.ServerServiceState = ServerServiceState.inactive;
            else if (state == "disabled")
                pavlovServer.ServerServiceState = ServerServiceState.disabled;
            else
                pavlovServer.ServerServiceState = ServerServiceState.none;

            return pavlovServer;
        }

        private void OverwrideTheNormalSSHLoginData(PavlovServerViewModel server, SshServer oldSSHcrid)
        {
            server.SshServer.SshPassphrase = oldSSHcrid.SshPassphrase;
            server.SshServer.SshUsername = oldSSHcrid.SshUsername;
            server.SshServer.SshPassword = oldSSHcrid.SshPassword;
            server.SshServer.SshKeyFileName = oldSSHcrid.SshKeyFileName;
        }


        public async Task IsValidOnly(PavlovServer pavlovServer, bool parseMd5 = true)
        {
            if (string.IsNullOrEmpty(pavlovServer.TelnetPassword) && pavlovServer.Id != 0)
                pavlovServer.TelnetPassword = (await FindOne(pavlovServer.Id)).TelnetPassword;
            if (!RconHelper.IsMD5(pavlovServer.TelnetPassword))
            {
                if (string.IsNullOrEmpty(pavlovServer.TelnetPassword))
                    throw new SaveServerException("Password", "The telnet password is required!");

                if (parseMd5)
                    pavlovServer.TelnetPassword = RconHelper.CreateMD5(pavlovServer.TelnetPassword);
            }

            if (pavlovServer.SshServer.SshPort <= 0) throw new SaveServerException("SshPort", "You need a SSH port!");

            if (string.IsNullOrEmpty(pavlovServer.SshServer.SshUsername))
                throw new SaveServerException("SshUsername", "You need a username!");


            if (string.IsNullOrEmpty(pavlovServer.SshServer.SshPassword) &&
                string.IsNullOrEmpty(pavlovServer.SshServer.SshKeyFileName))
                throw new SaveServerException("SshPassword", "You need at least a password or a key file!");
        }

        public virtual async Task<PavlovServer> ValidatePavlovServer(PavlovServer pavlovServer)
        {
            Console.WriteLine("start validate");
            var hasToStop = false;
            await IsValidOnly(pavlovServer);


            Console.WriteLine("try to start service");
            //try if the service realy exist
            try
            {
                pavlovServer = await GetServerServiceState(pavlovServer);
                if (pavlovServer.ServerServiceState != ServerServiceState.active)
                {
                    Console.WriteLine("has to start");
                    hasToStop = true;
                    //the problem is here for the validating part if it has to start the service first it has problems
                    await RconStatic.SystemDStart(pavlovServer,this);
                    pavlovServer = await GetServerServiceState(pavlovServer);

                    Console.WriteLine("state = " + pavlovServer.ServerServiceState);
                }
            }
            catch (CommandException e)
            {
                throw new SaveServerException("", e.Message);
            }

            Console.WriteLine("try to send serverinfo");
            //try to send Command ServerInfo
            try
            {
                var response = await RconStatic.SendCommandSShTunnel(pavlovServer, "ServerInfo");
            }
            catch (CommandException e)
            {
                await HasToStop(pavlovServer, hasToStop);
                throw new SaveServerException("", e.Message);
            }

            //try if the user have rights to delete maps cache
            try
            {
                Console.WriteLine("delete unused maps");
                RconStatic.DeleteUnusedMaps(pavlovServer,
                    (await _serverSelectedMapService.FindAllFrom(pavlovServer)).ToList());
            }
            catch (CommandException e)
            {
                await HasToStop(pavlovServer, hasToStop);
                throw new SaveServerException("", e.Message);
            }

            await HasToStop(pavlovServer, hasToStop);

            return pavlovServer;
        }

        private async Task<PavlovServer> HasToStop(PavlovServer pavlovServer, bool hasToStop)
        {
            if (hasToStop)
            {
                Console.WriteLine("stop server again!");
                await RconStatic.SystemDStop(pavlovServer,this);
                pavlovServer = await GetServerServiceState(pavlovServer);
            }

            return pavlovServer;
        }

        public async Task<bool> Upsert(PavlovServer pavlovServer, bool withCheck = true)
        {
            if (withCheck)
                pavlovServer = await ValidatePavlovServer(pavlovServer);
            return await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .UpsertAsync(pavlovServer);
        }

        public async Task<bool> Delete(int id)
        {
            var server = await FindOne(id);
            await _serverSelectedMapService.DeleteFromServer(server);
            await _serverSelectedModsService.DeleteFromServer(server);
            await _serverSelectedWhitelistService.DeleteFromServer(server);
            return await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer").DeleteAsync(server.Id);
        }
    }
}