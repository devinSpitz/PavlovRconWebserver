using System;
using System.Collections.Generic;
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
    public class PavlovServerService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly ServerSelectedWhitelistService _serverSelectedWhitelistService;

        public PavlovServerService(ILiteDbIdentityAsyncContext liteDbContext,
            ServerSelectedModsService serverSelectedModsService,
            ServerSelectedWhitelistService serverSelectedWhitelistService,
            ServerSelectedMapService serverSelectedMapService,
            IToastifyService notyfService)
        {
            _notifyService = notyfService;
            _liteDb = liteDbContext;
            _serverSelectedMapService = serverSelectedMapService;
            _serverSelectedModsService = serverSelectedModsService;
            _serverSelectedWhitelistService = serverSelectedWhitelistService;
        }

        public IToastifyService _notifyService { get; }


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

        public async Task<PavlovServer[]> FindAllServerWhereTheUserHasRights(ClaimsPrincipal cp, LiteDbUser user)
        {
            //Team Admins and Mods get added as mods to the server. So they don't need to get handled here.
            var tmpServer = new List<PavlovServer>();
            
            var allServer = _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Include(x=>x.SshServer.Owner)
                .Include(x=>x.Owner)
                .Query();

            if (cp.IsInRole("Admin"))
            {
                return await allServer.ToArrayAsync();
            }
            if (cp.IsInRole("Mod")||cp.IsInRole("Captain"))
            {
                tmpServer.AddRange((await allServer.Where(x=>x.Owner== null && x.SshServer.Owner==null ).ToArrayAsync()).Where(y=>!tmpServer.Select(c=>c.Id).Contains(y.Id)));
            }
            if (cp.IsInRole("ServerRent"))
            {
                tmpServer.AddRange((await allServer.Where(x=>x.Owner != null && x.Owner.Id == user.Id ).ToArrayAsync()).Where(y=>!tmpServer.Select(c=>c.Id).Contains(y.Id)));
            }
            if (cp.IsInRole("OnPremise"))
            {
                tmpServer.AddRange((await allServer.Where(x => x.SshServer.Owner != null && x.SshServer.Owner.Id == user.Id).ToArrayAsync()).Where(y=>!tmpServer.Select(c=>c.Id).Contains(y.Id)));
            }

            if (await IsModSomeWhere(user,_serverSelectedModsService))
            {
                var serversIds = (await _serverSelectedModsService.FindAllFrom(user)).Select(x=>x.PavlovServer.Id).ToArray();
                tmpServer.AddRange((await allServer.Where(x => serversIds.Contains(x.Id)).ToArrayAsync()).Where(y=>!tmpServer.Select(c=>c.Id).Contains(y.Id)));
            }
            
            
            
            return tmpServer.ToArray();

        }
        
        public async Task<PavlovServer[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Include(x=>x.SshServer.Owner)
                .Include(x=>x.Owner)
                .FindAllAsync()).OrderByDescending(x => x.Id).ToArray();
        }

        public async Task<PavlovServer[]> FindAllFrom(int sshServerId)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Include(x=>x.SshServer.Owner)
                .Include(x=>x.Owner)
                .FindAsync(x => x.SshServer.Id == sshServerId)).ToArray();
        }

        public async Task<PavlovServer> FindOne(int id)
        {
            var pavlovServer = await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .Include(x => x.SshServer)
                .Include(x=>x.SshServer.Owner)
                .Include(x=>x.Owner)
                .FindOneAsync(x => x.Id == id);
            return pavlovServer;
        }


        public async Task<KeyValuePair<PavlovServerViewModel, string>> CreatePavlovServer(PavlovServerViewModel server)
        {
            //Todo: The hole chain of this function is just bad. To less error handling etc. Have to make this better in the future.
            string result = null;
            try
            {
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start creting server", LogEventLevel.Verbose,
                    _notifyService);
                //Check stuff
                var exist = RconStatic.DoesPathExist(server, server.ServerFolderPath, _notifyService);
                if (exist == "true") throw new CommandExceptionCreateServerDuplicate("ServerFolderPath already exist!");

                exist = RconStatic.DoesPathExist(server,
                    "/etc/systemd/system/" + server.ServerSystemdServiceName + ".service", _notifyService);
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


                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start Install pavlovserver", LogEventLevel.Verbose,
                    _notifyService);

                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                    "Username used befor changed to root: " + server.SshServer.SshUsername, LogEventLevel.Verbose,
                    _notifyService);
                result += await RconStatic.UpdateInstallPavlovServer(server, this);
                result += "\n *******************************Update/Install Done*******************************";

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start Install pavlovserver service",
                    LogEventLevel.Verbose, _notifyService);

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
                    result += await RconStatic.InstallPavlovServerService(server, _notifyService, this);
                }
                catch (CommandException e)
                {
                    //If crash inside here the user login is still root. If the root login is bad this will fail to remove the server afterwards
                    OverwrideTheNormalSSHLoginData(server, oldSSHcrid);
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                        "catch after install override and remove afterwards: "+e.Message, LogEventLevel.Verbose, _notifyService);
                    throw;
                }

                OverwrideTheNormalSSHLoginData(server, oldSSHcrid);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify(
                    "Username used after override old infos: " + server.SshServer.SshUsername, LogEventLevel.Verbose,
                    _notifyService);
                //start server and stop server to get Saved folder etc.

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Start after install", LogEventLevel.Verbose,
                    _notifyService);
                try
                {
                    await RconStatic.SystemDStart(server, this);
                }
                catch (Exception)
                {
                    //ignore
                }

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("stop after install", LogEventLevel.Verbose,
                    _notifyService);
                try
                {
                    await RconStatic.SystemDStop(server, this);
                }
                catch (Exception)
                {
                    //ignore
                }

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("Try to save game ini", LogEventLevel.Verbose,
                    _notifyService);
                result +=
                    "\n *******************************Update/Install PavlovServerService Done*******************************";

                var pavlovServerGameIni = new PavlovServerGameIni();

                DataBaseLogger.LogToDatabaseAndResultPlusNotify("created Ini", LogEventLevel.Verbose, _notifyService);
                var selectedMaps = await _serverSelectedMapService.FindAllFrom(server);
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("found maps", LogEventLevel.Verbose, _notifyService);
                pavlovServerGameIni.ServerName = server.Name;
                result += pavlovServerGameIni.SaveToFile(server, selectedMaps, _notifyService);
                result += "\n *******************************Save server settings Done*******************************";
                //also create rcon settings


                DataBaseLogger.LogToDatabaseAndResultPlusNotify("write rcon file", LogEventLevel.Verbose,
                    _notifyService);
                var rconSettingsTempalte = "Password=" + server.TelnetPassword + "\nPort=" + server.TelnetPort;
                result += RconStatic.WriteFile(server, server.ServerFolderPath + FilePaths.RconSettings,
                    rconSettingsTempalte, _notifyService);


                result += "\n *******************************create rconSettings Done*******************************";

                DataBaseLogger.LogToDatabaseAndResultPlusNotify(result, LogEventLevel.Verbose, _notifyService);
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
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify(e.Message, LogEventLevel.Verbose, _notifyService);
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
        public async Task<PavlovServer> GetServerServiceState(PavlovServer pavlovServer)
        {
            var state = await RconStatic.SystemDCheckState(pavlovServer, _notifyService);
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

        private PavlovServerViewModel OverwrideTheNormalSSHLoginData(PavlovServerViewModel server, SshServer oldSSHcrid)
        {
            server.SshServer.SshPassphrase = oldSSHcrid.SshPassphrase;
            server.SshServer.SshUsername = oldSSHcrid.SshUsername;
            server.SshServer.SshPassword = oldSSHcrid.SshPassword;
            server.SshServer.SshKeyFileName = oldSSHcrid.SshKeyFileName;
            return server;
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
                (pavlovServer.SshServer.SshKeyFileName==null||!pavlovServer.SshServer.SshKeyFileName.Any()))
                throw new SaveServerException("SshPassword", "You need at least a password or a key file!");
        }

        public virtual async Task<PavlovServer> ValidatePavlovServer(PavlovServer pavlovServer, bool root)
        {
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("start validate", LogEventLevel.Verbose, _notifyService);
            var hasToStop = false;
            await IsValidOnly(pavlovServer);


            DataBaseLogger.LogToDatabaseAndResultPlusNotify("try to start service", LogEventLevel.Verbose,
                _notifyService);
            //try if the service realy exist
            try
            {
                pavlovServer = await GetServerServiceState(pavlovServer);
                if (pavlovServer.ServerServiceState != ServerServiceState.active)
                {
                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("has to start", LogEventLevel.Verbose,
                        _notifyService);
                    hasToStop = true;
                    //the problem is here for the validating part if it has to start the service first it has problems
                    await RconStatic.SystemDStart(pavlovServer, this);
                    pavlovServer = await GetServerServiceState(pavlovServer);

                    DataBaseLogger.LogToDatabaseAndResultPlusNotify("state = " + pavlovServer.ServerServiceState,
                        LogEventLevel.Verbose, _notifyService);
                }
            }
            catch (CommandException e)
            {
                throw new SaveServerException("", e.Message);
            }

            DataBaseLogger.LogToDatabaseAndResultPlusNotify("try to send serverinfo", LogEventLevel.Verbose,
                _notifyService);
            //try to send Command ServerInfo
            try
            {
                var response = await RconStatic.SendCommandSShTunnel(pavlovServer, "ServerInfo", _notifyService);
            }
            catch (CommandException e)
            {
                await HasToStop(pavlovServer, hasToStop, root);
                throw new SaveServerException("", e.Message);
            }

            //try if the user have rights to delete maps cache
            try
            {
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("delete unused maps", LogEventLevel.Verbose,
                    _notifyService);
                RconStatic.DeleteUnusedMaps(pavlovServer,
                    (await _serverSelectedMapService.FindAllFrom(pavlovServer)).ToList());
            }
            catch (CommandException e)
            {
                await HasToStop(pavlovServer, hasToStop, root);
                throw new SaveServerException("", e.Message);
            }

            await HasToStop(pavlovServer, hasToStop, root);

            return pavlovServer;
        }

        private async Task<PavlovServer> HasToStop(PavlovServer pavlovServer, bool hasToStop, bool root)
        {
            if (hasToStop)
            {
                DataBaseLogger.LogToDatabaseAndResultPlusNotify("stop server again!", LogEventLevel.Verbose,
                    _notifyService);
                await RconStatic.SystemDStop(pavlovServer, this);
                pavlovServer = await GetServerServiceState(pavlovServer);
            }

            return pavlovServer;
        }

        public async Task<PavlovServer> Upsert(PavlovServer pavlovServer, bool withCheck = true)
        {
            if (withCheck)
                pavlovServer = await ValidatePavlovServer(pavlovServer, false);
            var result = await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                .UpsertAsync(pavlovServer);
            if (result)
                await _liteDb.LiteDatabaseAsync.GetCollection<PavlovServer>("PavlovServer")
                    .FindOneAsync(x => x.Id == pavlovServer.Id);

            return pavlovServer;
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