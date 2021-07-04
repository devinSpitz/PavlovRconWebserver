using System;
using System.Threading.Tasks;
using LiteDB.Identity.Database;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public static class SystemdService
    { 
        
        public static async Task CheckServiceStateForAll(string connectionString)
        {
            
            var steamIdentityService = new SteamIdentityService(new LiteDbIdentityContext(connectionString));
            var serverSelectedMapService = new ServerSelectedMapService(new LiteDbIdentityContext(connectionString));
            var pavlovServerService = new PavlovServerService(new LiteDbIdentityContext(connectionString));
            var sshServerSerivce = new SshServerSerivce(new LiteDbIdentityContext(connectionString),pavlovServerService);
            var mapsService = new MapsService(new LiteDbIdentityContext(connectionString));
            var pavlovServerInfoService = new PavlovServerInfoService(new LiteDbIdentityContext(connectionString),pavlovServerService,mapsService);
            var pavlovServerPlayerService = new PavlovServerPlayerService(new LiteDbIdentityContext(connectionString),pavlovServerService,pavlovServerInfoService);
            var pavlovServerPlayerHistoryService = new PavlovServerPlayerHistoryService(new LiteDbIdentityContext(connectionString));
            var rconSerivce = new RconService(steamIdentityService,serverSelectedMapService,mapsService,pavlovServerInfoService,pavlovServerPlayerService,pavlovServerPlayerHistoryService);
            var servers = await sshServerSerivce.FindAll();
            foreach (var server in servers)
            {
                
                foreach (var signleServer in server.PavlovServers)
                {
                    try
                    {
                        await UpdateServerState(signleServer, rconSerivce, pavlovServerService, sshServerSerivce,false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    } 
                }
            }
        }

        public static async Task UpdateServerState(PavlovServer signleServer, RconService rconSerivce,
            PavlovServerService pavlovServerService, SshServerSerivce sshServerSerivce,bool withCheck)
        {
            var serverWithState = await GetServerServiceState(signleServer, rconSerivce);
            await pavlovServerService.Upsert(serverWithState, rconSerivce, sshServerSerivce,withCheck);
        }        
        
        public static async Task UpdateAllServiceStates(RconService rconSerivce,PavlovServerService pavlovServerService, SshServerSerivce sshServerSerivce)
        {
            var servers = await sshServerSerivce.FindAll();
            foreach (var sshServer in servers)
            {
                foreach (var pavlovServer in sshServer.PavlovServers)
                {
                    try
                    {
                        await UpdateServerState(pavlovServer, rconSerivce,pavlovServerService,sshServerSerivce,false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    } 
                }
            }
        }

        /// <summary>
        ///
        /// 
        /// </summary>
        /// <param name="pavlovServer"></param>
        /// <param name="rconService"></param>
        /// <returns></returns>
        public static async Task<PavlovServer> GetServerServiceState(PavlovServer pavlovServer, RconService rconService)
        {
            var state = await rconService.SendCommand(pavlovServer, "", false, false, "", false, false, null, false, true);
            // state can be: active, inactive,disabled
            if (state == "active")
            {
                pavlovServer.ServerServiceState = ServerServiceState.active;
            }
            else if (state == "inactive")
            {
                pavlovServer.ServerServiceState = ServerServiceState.inactive;
            }
            else if (state == "disabled")
            {
                pavlovServer.ServerServiceState = ServerServiceState.disabled;
            }
            else
            {
                pavlovServer.ServerServiceState = ServerServiceState.none;
            }

            return pavlovServer;
        }
        
        public static async Task<string> StopServerService(PavlovServer pavlovServer,RconService rconSerivce,PavlovServerService pavlovServerService, SshServerSerivce sshServerSerivce)
        {
            var state = await rconSerivce.SendCommand(pavlovServer, "", false, false, "", false, false, null, false, false,false,true);
            await UpdateAllServiceStates(rconSerivce, pavlovServerService, sshServerSerivce);
            return state;
        }
                
        public static async Task<string> StartServerService(PavlovServer pavlovServer, RconService rconSerivce,PavlovServerService pavlovServerService, SshServerSerivce sshServerSerivce)
        {
            var state = await rconSerivce.SendCommand(pavlovServer, "", false, false, "", false, false, null, false, false,true);
            await UpdateAllServiceStates(rconSerivce, pavlovServerService, sshServerSerivce);
            return state;
        }

        
    }
}