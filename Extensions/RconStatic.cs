using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class RconStatic
    { 
        
        public static async Task CheckBansForAllServers(string connectionString)
        {
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var rconSerivce = new RconService(serverSelectedMapService,sshServerSerivce);
            var serverBansService = new ServerBansService(new LiteDbIdentityContext(connectionString));
            var servers = await sshServerSerivce.FindAll();
            foreach (var server in servers)
            {
                
                foreach (var signleServer in server.PavlovServers)
                {
                    try
                    {
                        var bans = await serverBansService.FindAllFromPavlovServerId(signleServer.Id,true);
                        await rconSerivce.SaveBlackListEntry(signleServer, bans);
                    }
                    catch (Exception e)
                    {
                        // ingore for now
                    } 
                }
            }
        }

    }
}