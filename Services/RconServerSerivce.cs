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
        public RconServerSerivce(ILiteDbIdentityContext liteDbContext)
        {
            _liteDb = liteDbContext;
        }

        public async Task<IEnumerable<RconServer>> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Include(x=>x.PavlovServers)
                .FindAll();
        }

        public async Task<RconServer> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<int> Insert(RconServer rconServer,RconService service)
        {

            foreach (var singleServer in rconServer.PavlovServers)
            {
                
                await validateRconServer(singleServer,service);
            }
            
            
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Insert(rconServer);
        }

        public async Task validateRconServer(PavlovServer rconServer,RconService rconService)
        {

            if (!RconHelper.IsMD5(rconServer.TelnetPassword))
            {
                if (String.IsNullOrEmpty(rconServer.TelnetPassword))
                {
                    throw new SaveServerException("Password","The telnet password is required!");
                }
                rconServer.TelnetPassword = RconHelper.CreateMD5(rconServer.TelnetPassword);
            }

            if (rconServer.RconServer.SshPort<=0)
            {
                throw new SaveServerException("SshPort","You need a SSH port!");
            }

            if (String.IsNullOrEmpty(rconServer.RconServer.SshUsername))
            {
                throw new SaveServerException("SshUsername","You need a username!");
            }
            
            if (String.IsNullOrEmpty(rconServer.RconServer.SshPassword)&&String.IsNullOrEmpty(rconServer.RconServer.SshKeyFileName))
            {
                throw new SaveServerException("SshPassword","You need at least a password or a key file!");
            }
            //try to send Command ServerInfo
            try
            {
                var response = await rconService.SendCommand(rconServer, "ServerInfo");
            }
            catch (CommandException e)
            {
                throw new SaveServerException("",e.Message);
            }
            //try if the user have rights to delete maps cache
            try
            {
                await rconService.SendCommand(rconServer, "", true);
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