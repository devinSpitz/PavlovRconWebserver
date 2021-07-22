using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class SshServerSerivce
    {
        private readonly ILiteDbIdentityContext _liteDb;
        private readonly PavlovServerService _pavlovServer;

        public SshServerSerivce(ILiteDbIdentityContext liteDbContext, PavlovServerService pavlovServerService)
        {
            _liteDb = liteDbContext;
            _pavlovServer = pavlovServerService;
        }

        public async Task<IEnumerable<SshServer>> FindAll()
        {
            var list = _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .FindAll().Select(x =>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).ToList();
            return list;
        }

        public async Task<SshServer> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Find(x => x.Id == id).Select(x =>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).FirstOrDefault();
        }

        public async Task<int> Insert(SshServer sshServer, RconService service)
        {
            await validateSshServer(sshServer, service);
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Insert(sshServer);
        }

        public async Task validateSshServer(SshServer server, RconService rconService)
        {
            if (server.SshPort <= 0) throw new SaveServerException("SshPort", "You need a SSH port!");

            if (string.IsNullOrEmpty(server.SshUsername))
                throw new SaveServerException("SshUsername", "You need a username!");

            if (string.IsNullOrEmpty(server.SshPassword) && string.IsNullOrEmpty(server.SshKeyFileName))
                throw new SaveServerException("SshPassword", "You need at least a password or a key file!");
        }

        public async Task<PavlovServer> validatePavlovServer(PavlovServer pavlovServer, RconService rconService)
        {
            
            Console.WriteLine("start validate");
            var hasToStop = false;
            if (string.IsNullOrEmpty(pavlovServer.TelnetPassword) && pavlovServer.Id != 0)
                pavlovServer.TelnetPassword = (await _pavlovServer.FindOne(pavlovServer.Id)).TelnetPassword;
            if (!RconHelper.IsMD5(pavlovServer.TelnetPassword))
            {
                if (string.IsNullOrEmpty(pavlovServer.TelnetPassword))
                    throw new SaveServerException("Password", "The telnet password is required!");

                pavlovServer.TelnetPassword = RconHelper.CreateMD5(pavlovServer.TelnetPassword);
            }

            if (pavlovServer.SshServer.SshPort <= 0) throw new SaveServerException("SshPort", "You need a SSH port!");

            if (string.IsNullOrEmpty(pavlovServer.SshServer.SshUsername))
                throw new SaveServerException("SshUsername", "You need a username!");

            if (string.IsNullOrEmpty(pavlovServer.SshServer.SshPassword) &&
                string.IsNullOrEmpty(pavlovServer.SshServer.SshKeyFileName))
                throw new SaveServerException("SshPassword", "You need at least a password or a key file!");

            
            Console.WriteLine("try to start service");
            //try if the service realy exist
            try
            {
                pavlovServer = await SystemdService.GetServerServiceState(pavlovServer, rconService);
                if (pavlovServer.ServerServiceState != ServerServiceState.active)
                {
                    Console.WriteLine("has to start");
                    hasToStop = true;
                    //the problem is here for the validating part if it has to start the service first it has problems
                    await rconService.SystemDStart(pavlovServer);
                    pavlovServer = await SystemdService.GetServerServiceState(pavlovServer, rconService);
                    
                    Console.WriteLine("state = "+pavlovServer.ServerServiceState);
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
                var response = await rconService.SendCommandSShTunnel(pavlovServer, "ServerInfo");
            }
            catch (CommandException e)
            {
                await HasToStop(pavlovServer, rconService, hasToStop);
                throw new SaveServerException("", e.Message);
            }

            //try if the user have rights to delete maps cache
            try
            {
                Console.WriteLine("delete unused maps");
                await rconService.DeleteUnusedMaps(pavlovServer);
            }
            catch (CommandException e)
            { 
                await HasToStop(pavlovServer, rconService, hasToStop);
                throw new SaveServerException("", e.Message);
            }
            await HasToStop(pavlovServer, rconService, hasToStop);

            return pavlovServer;
        }

        private async Task<PavlovServer> HasToStop(PavlovServer pavlovServer, RconService rconService, bool hasToStop)
        {
            if (hasToStop)
            {
                Console.WriteLine("stop server again!");
                await rconService.SystemDStop(pavlovServer);
                pavlovServer = await SystemdService.GetServerServiceState(pavlovServer, rconService);
            }

            return pavlovServer;
        }


        public async Task<bool> Update(SshServer sshServer, RconService rconService)
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

            await validateSshServer(sshServer, rconService);

            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Update(sshServer);
        }

        public async Task<bool> Delete(int id, PavlovServerService pavlovServerService,
            ServerSelectedWhitelistService serverSelectedWhitelistService,
            ServerSelectedMapService serverSelectedMapService, ServerSelectedModsService serverSelectedModsService)
        {
            var pavlovServers = (await _pavlovServer.FindAll()).Where(x => x.SshServer.Id == id);
            foreach (var pavlovServer in pavlovServers)
                await pavlovServerService.Delete(pavlovServer.Id, serverSelectedWhitelistService,
                    serverSelectedMapService, serverSelectedModsService);
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer").Delete(id);
        }
    }
}