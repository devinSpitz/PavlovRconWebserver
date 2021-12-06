using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Hangfire;
using LiteDB;
using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Models;
using Serilog.Sinks.Models;

namespace PavlovRconWebserver.Services
{
    public class ReservedServersService
    {
        private readonly ILiteDbIdentityAsyncContext _liteDb;
        private readonly PavlovServerService _pavlovServerService;        
        private readonly SshServerSerivce _sshServerService;        
        private readonly IToastifyService _notifyService;        
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly UserService _userService;

        public ReservedServersService(ILiteDbIdentityAsyncContext liteDbContext,
            UserManager<LiteDbUser> userManager,
                UserService userService,
                PavlovServerService pavlovServerService,
                SshServerSerivce sshServerService,
            IToastifyService notyfService)
        {
            _userManager = userManager;
            _userService = userService;
            _pavlovServerService = pavlovServerService;
            _sshServerService = sshServerService;
            _notifyService = notyfService;
            _liteDb = liteDbContext;
        }

        public async Task<ReservedServer[]> FindAll()
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").FindAllAsync()).ToArray();
        }        
        
        public async Task<ReservedServer[]> FindByEmail(string email)
        {
            return (await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").FindAllAsync()).Where(x=>x.Email==email).ToArray();
        }        
        public async Task<bool> Remove(int id)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer").DeleteAsync(id);
        }    
        
        
        public async Task CheckReservedServedToGiveToAUser()
        {
            var reserved = await FindAll();
            if (reserved.Any())
            {
                foreach (var reservedServer in reserved)
                {
                    var user = await _userService.GetUserByEmail(reservedServer.Email);
                    if(user==null) continue;
                    if (reservedServer.ServerId != null)
                    {
                        var server = await _pavlovServerService.FindOne((int)reservedServer.ServerId);
                        if (server == null) continue;
                        server.Owner = user;
                        server.LiteDbUserId = user.Id.ToString();
                        await _pavlovServerService.Upsert(server);
                        if(!await _userManager.IsInRoleAsync(user,"ServerRent"))
                            await _userManager.AddToRoleAsync(user, "ServerRent");
                    }
                    else if(reservedServer.SshServerId != null)
                    {
                        var server = await _sshServerService.FindOne((int)reservedServer.SshServerId);
                        if (server == null) continue;
                        server.Owner = user;
                        server.LiteDbUserId = user.Id.ToString();
                        await _sshServerService.Update(server);
                        if(!await _userManager.IsInRoleAsync(user,"OnPremise"))
                            await _userManager.AddToRoleAsync(user, "OnPremise");
                    }
                    await Remove(reservedServer.Id);
                }

            }
            BackgroundJob.Schedule(
                () => CheckReservedServedToGiveToAUser(),new TimeSpan(0,5,0)); 
        }    
        public async Task<bool> Add(ReservedServer reservedServer)
        {
            return await _liteDb.LiteDatabaseAsync.GetCollection<ReservedServer>("ReservedServer")
                .UpsertAsync(reservedServer);
        }
    }
    
}