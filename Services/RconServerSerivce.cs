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
                .FindAll();
        }

        public async Task<RconServer> FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public async Task<int> Insert(RconServer rconServer,RconService service)
        {

            
            rconServer = await validateRconServer(rconServer,service);
            
            
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Insert(rconServer);
        }

        private async Task<RconServer> validateRconServer(RconServer rconServer,RconService rconService)
        {

            if (!RconHelper.IsMD5(rconServer.Password))
            {
                if (String.IsNullOrEmpty(rconServer.Password))
                {
                    throw new SaveServerException("Password","The telnet password is required!");
                }
                rconServer.Password = RconHelper.CreateMD5(rconServer.Password);
            }

            if (rconServer.SshPort<=0)
            {
                throw new SaveServerException("SshPort","You need a SSH port!");
            }

            if (String.IsNullOrEmpty(rconServer.SshUsername))
            {
                throw new SaveServerException("SshUsername","You need a username!");
            }
            
            if (String.IsNullOrEmpty(rconServer.SshPassword)&&String.IsNullOrEmpty(rconServer.SshKeyFileName))
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
            // test Command ServerInfo
            return rconServer;

        }

        public async Task<bool> Update(RconServer rconServer,RconService rconService)
        {
            RconServer old = null;
            if (string.IsNullOrEmpty(rconServer.Password) || string.IsNullOrEmpty(rconServer.SshPassphrase) || string.IsNullOrEmpty(rconServer.SshPassword))
            {
                old = await FindOne(rconServer.Id);
                if (old != null)
                {
                    if (string.IsNullOrEmpty(rconServer.Password)) {rconServer.Password = old.Password;}
                    if (string.IsNullOrEmpty(rconServer.SshPassphrase)) rconServer.SshPassphrase = old.SshPassphrase;
                    if (string.IsNullOrEmpty(rconServer.SshPassword)) rconServer.SshPassword = old.SshPassword;
                }
            }

            rconServer = await validateRconServer(rconServer,rconService);
            //Check for needed combinations

            
               
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Update(rconServer); 
            
           
        }

        public async Task<bool> Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer").Delete(id);
        }
    }
}