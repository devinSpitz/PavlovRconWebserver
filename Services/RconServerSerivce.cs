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
    public class RconServerSerivce
    {
        private ILiteDbIdentityContext _liteDb;
        private PavlovServerService _pavlovServer;
        public RconServerSerivce(ILiteDbIdentityContext liteDbContext,PavlovServerService pavlovServerService)
        {
            _liteDb = liteDbContext;
            _pavlovServer = pavlovServerService;
        }

        public async Task<IEnumerable<RconServer>> FindAll()
        {
            var list =  _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .FindAll().Select(x=>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).ToList();
            return list;
        }

        public async Task<RconServer> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Find(x => x.Id == id).Select(x=>
                {
                    x.PavlovServers = _pavlovServer.FindAllFrom(x.Id);
                    return x;
                }).FirstOrDefault();
        }

        public async Task<int> Insert(RconServer rconServer,RconService service)
        {
            if(rconServer.PavlovServers != null)
                foreach (var singleServer in rconServer.PavlovServers)
                {
                    await validateRconServer(singleServer,service);
                }
            
            
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Insert(rconServer);
        }

        public async Task validateRconServer(PavlovServer pavlovServer,RconService rconService)
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

            if (pavlovServer.RconServer.SshPort<=0)
            {
                throw new SaveServerException("SshPort","You need a SSH port!");
            }

            if (String.IsNullOrEmpty(pavlovServer.RconServer.SshUsername))
            {
                throw new SaveServerException("SshUsername","You need a username!");
            }
            
            if (String.IsNullOrEmpty(pavlovServer.RconServer.SshPassword)&&String.IsNullOrEmpty(pavlovServer.RconServer.SshKeyFileName))
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

        public async Task<bool> Update(RconServer rconServer,RconService rconService)
        {
            RconServer old = null;
            if (string.IsNullOrEmpty(rconServer.SshPassphrase) || string.IsNullOrEmpty(rconServer.SshPassword))
            {
                old = await FindOne(rconServer.Id);
                if (old != null)
                {
                    if (string.IsNullOrEmpty(rconServer.SshPassphrase)) rconServer.SshPassphrase = old.SshPassphrase;
                    if (string.IsNullOrEmpty(rconServer.SshPassword)) rconServer.SshPassword = old.SshPassword;
                }
            }
            foreach (var singleServer in rconServer.PavlovServers)
            {
                await validateRconServer(singleServer,rconService);
            }

            
               
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Update(rconServer); 
            
           
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer").Delete(id);
        }
    }
}