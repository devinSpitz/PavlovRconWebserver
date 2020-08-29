using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using LiteDB.Identity.Database;
using Microsoft.AspNetCore.Razor.Language.Extensions;
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

        public IEnumerable<RconServer> FindAll()
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .FindAll();
        }

        public RconServer FindOne(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public int Insert(RconServer rconServer)
        {

            
            rconServer = validateRconServer(rconServer);
            
            
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Insert(rconServer);
        }

        private RconServer validateRconServer(RconServer rconServer)
        {
            if (!rconServer.UseSsh && !rconServer.UseTelnet)
            {
                throw new SaveServerException("UseSsh","You need at least one connection type! Please choose SSH or Telnet.");
            }
            
            if (!RconHelper.IsMD5(rconServer.Password))
            {
                if (String.IsNullOrEmpty(rconServer.Password))
                {
                    throw new SaveServerException("Password","The telnet password is required!");
                }
                rconServer.Password = RconHelper.CreateMD5(rconServer.Password);
            }
            
            if (rconServer.UseSsh)
            {
                if (rconServer.SshPort<=0)
                {
                    throw new SaveServerException("SshPort","If you use SSH you will need a SSH port!");
                }

                if (String.IsNullOrEmpty(rconServer.SshUsername))
                {
                    throw new SaveServerException("SshUsername","If you use SSH you will need a username!");
                }
                
                if (String.IsNullOrEmpty(rconServer.SshPassword)&&String.IsNullOrEmpty(rconServer.SshKeyFileName))
                {
                    throw new SaveServerException("SshPassword","If you use SSH you will need at least a password or a key file!");
                }
            }

            return rconServer;

        }

        public bool Update(RconServer rconServer)
        {
            RconServer old = null;
            if (string.IsNullOrEmpty(rconServer.Password) || string.IsNullOrEmpty(rconServer.SshPassphrase) || string.IsNullOrEmpty(rconServer.SshPassword))
            {
                old = FindOne(rconServer.Id);
                if (old != null)
                {
                    if (string.IsNullOrEmpty(rconServer.Password)) {rconServer.Password = old.Password;}
                    if (string.IsNullOrEmpty(rconServer.SshPassphrase)) rconServer.SshPassphrase = old.SshPassphrase;
                    if (string.IsNullOrEmpty(rconServer.SshPassword)) rconServer.SshPassword = old.SshPassword;
                }
            }

            rconServer = validateRconServer(rconServer);
            //Check for needed combinations

            
               
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer")
                .Update(rconServer); 
            
           
        }

        public bool Delete(int id)
        {
            return _liteDb.LiteDatabase.GetCollection<RconServer>("RconServer").Delete(id);
        }
    }
}