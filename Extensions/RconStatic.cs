using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire;
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
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,mapsService);
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,pavlovServerInfoService);
            var pavlovServerPlayerHistoryService = new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconSerivce = new RconService(serverSelectedMapService,mapsService,pavlovServerInfoService,pavlovServerPlayerService,pavlovServerPlayerHistoryService);
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
        
        public static async Task ReloadPlayerListFromServerAndTheServerInfo(string connectionString,bool recursive = false)
        {
            var exceptions = new List<Exception>();
            try
            {
                var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
                var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
                var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
                var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
                var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,mapsService);
                var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,pavlovServerInfoService);
                var pavlovServerPlayerHistoryService = new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
                var rconSerivce = new RconService(serverSelectedMapService,mapsService,pavlovServerInfoService,pavlovServerPlayerService,pavlovServerPlayerHistoryService);
                var servers = await sshServerSerivce.FindAll();
                foreach (var server in servers)
                {
                    foreach (var signleServer in server.PavlovServers.Where(x=>x.ServerType == ServerType.Community))
                    {
                        try
                        {
                            await rconSerivce.SendCommand(signleServer,"",false,false,"",false,false,null,true);
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        } 
                    }
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            // Ignore them for now
            // if (exceptions.Count > 0)
            // {
            //     throw new Exception(String.Join(" | Next Exception:  ",exceptions.Select(x=>x.Message).ToList()));
            // }

            BackgroundJob.Schedule(() => ReloadPlayerListFromServerAndTheServerInfo(connectionString, recursive),new TimeSpan(0, 1, 0)); // Check for bans and remove them is necessary

        }
        

    }
}