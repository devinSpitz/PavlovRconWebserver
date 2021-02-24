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
        
        public static async Task ReloadPlayerListFromServerAndTheServerInfo(string connectionString,bool recursive = false)
        {
            try
            {
                var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
                var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
                var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
                var rconSerivce = new RconService(serverSelectedMapService,sshServerSerivce);
                var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
                var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,rconSerivce,mapsService);
                var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,rconSerivce,pavlovServerInfoService);
                var servers = await sshServerSerivce.FindAll();
                foreach (var server in servers)
                {
                    foreach (var signleServer in server.PavlovServers)
                    {
                        try
                        {
                            await pavlovServerInfoService.SaveRealServerInfoFromServer(signleServer.Id);
                            await pavlovServerPlayerService.SaveRealTimePlayerListFromServer(signleServer.Id);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            // ingore for now
                        } 
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); // not complete ignore at least let us now that there is a problem over cli
                //But the Job has to go one cause otherwise the playerlist is not updated
            }
            if(recursive)
                BackgroundJob.Schedule(() => ReloadPlayerListFromServerAndTheServerInfo(connectionString,recursive),new TimeSpan(0,0,30)); // Check for bans and remove them is necessary
        }
        

    }
}