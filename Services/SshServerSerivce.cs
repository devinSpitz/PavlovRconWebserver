using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver.Services
{
    public class SshServerSerivce
    {
        private ILiteDbIdentityContext _liteDb;
        private PavlovServerService _pavlovServer;
        public SshServerSerivce(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService)
        {
            _liteDb = liteDbContext;
            _pavlovServer = pavlovServerService;
        }

        public async Task<IEnumerable<SshServer>> FindAll()
        {
            var list =  _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .FindAll().Select(x=>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).ToList();
            return list;
        }

        public async Task<SshServer> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Find(x => x.Id == id).Select(x=>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).FirstOrDefault();
        }

        public async Task<int> Insert(SshServer sshServer,RconService service)
        {
            if(sshServer.PavlovServers != null)
                foreach (var singleServer in sshServer.PavlovServers)
                {
                    await validateSshServer(singleServer,service);
                }
            
            
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Insert(sshServer);
        }

        public async Task validateSshServer(PavlovServer pavlovServer,RconService rconService)
        {
            if (String.IsNullOrEmpty(pavlovServer.TelnetPassword)&&pavlovServer.Id!=0)
            {
                pavlovServer.TelnetPassword = (await _pavlovServer.FindOne(pavlovServer.Id)).TelnetPassword;
            }
            if (!RconHelper.IsMD5(pavlovServer.TelnetPassword))
            {
                if (String.IsNullOrEmpty(pavlovServer.TelnetPassword))
                {
                    throw new SaveServerException("Password", "The telnet password is required!");
                }

                pavlovServer.TelnetPassword = RconHelper.CreateMD5(pavlovServer.TelnetPassword);
            }

            if (pavlovServer.SshServer.SshPort<=0)
            {
                throw new SaveServerException("SshPort","You need a SSH port!");
            }

            if (String.IsNullOrEmpty(pavlovServer.SshServer.SshUsername))
            {
                throw new SaveServerException("SshUsername","You need a username!");
            }
            
            if (String.IsNullOrEmpty(pavlovServer.SshServer.SshPassword)&&String.IsNullOrEmpty(pavlovServer.SshServer.SshKeyFileName))
            {
                throw new SaveServerException("SshPassword","You need at least a password or a key file!");
            }
            //try to send Command ServerInfo
            try
            {
                var response = await rconService.SendCommand(pavlovServer, "ServerInfo");
            }
            catch (CommandException e)
            {
                throw new SaveServerException("",e.Message);
            }
            //try if the user have rights to delete maps cache
            try
            {
                await rconService.SendCommand(pavlovServer, "",true);
            }
            catch (CommandException e)
            {
                throw new SaveServerException("",e.Message);
            }

        }

        public async Task<bool> Update(SshServer sshServer,RconService rconService)
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
            if(sshServer.PavlovServers != null)
                foreach (var singleServer in sshServer.PavlovServers)
                {
                    await validateSshServer(singleServer,rconService);
                }

            
               
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer")
                .Update(sshServer); 
            
           
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<SshServer>("SshServer").Delete(id);
        }
    }
}