
using System;
using System.Net;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PavlovRconWebserver.Services;
using LiteDB.Identity.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Models;

namespace PavlovRconWebserver
{
   public class Startup
   {
      public Startup(IConfiguration configuration) => Configuration = configuration;

      private IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         services.AddHangfire(x => x.UseMemoryStorage());
         string connectionString = Configuration.GetConnectionString("DefaultConnection");
         services.AddLiteDBIdentity(connectionString).AddDefaultTokenProviders();
         // Add LiteDB Dependency. Thare are three ways to set database:
         // 1. By default it uses the first connection string on appsettings.json, ConnectionStrings section.
         services.AddTransient<SshServerSerivce>();
         services.AddTransient<UserService>();
         services.AddTransient<RconService>();
         services.AddTransient<ServerSelectedMapService>();
         services.AddTransient<MapsService>();
         services.AddTransient<TeamService>();
         services.AddTransient<SteamIdentityService>();
         services.AddTransient<TeamSelectedSteamIdentityService>();
         services.AddTransient<MatchService>();
         services.AddTransient<PavlovServerService>();
         services.AddTransient<ServerBansService>();
         services.AddTransient<PavlovServerPlayerService>();
         services.AddTransient<PavlovServerInfoService>();
         services.AddTransient<PavlovServerPlayerHistoryService>();
         services.AddTransient<MatchSelectedSteamIdentitiesService>();
         services.AddTransient<MatchSelectedTeamSteamIdentitiesService>();
         services.AddTransient<ServerSelectedWhitelistService>();
         services.AddTransient<ServerSelectedModsService>();
         // services
         //    .AddAuthentication(cfg =>
         //    {
         //       cfg.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
         //    })
         //    .AddCookie();
         // Add application services.
         services.AddTransient<IEmailSender, EmailSender>();
         services.AddSwaggerGen(c =>
         {
            c.SwaggerDoc("v0.0.1", new OpenApiInfo { 
               Title = "Pavlov Rcon Webserver API", 
               Version = "v0.0.1",
               Description ="Here you can see all function of the PavlovRconWebserver"
            });
         });
         services.AddMvc().AddRazorRuntimeCompilation();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         
         if (env.IsDevelopment())
         {
            app.UseDeveloperExceptionPage();
         }
         else
         {
            app.UseExceptionHandler("/Home/Error");
         }
         var options = new BackgroundJobServerOptions
         {
            WorkerCount = 10

         };

         if (env.EnvironmentName == "Development")
         {
            app.UseHangfireDashboard("/hangfire"
               //    ,new DashboardOptions
               // {
               //    Authorization = new IDashboardAuthorizationFilter[]
               //    {
               //       new DashboardAuthorizationFilter()
               //    }
               // }
            ); // Does not work on dotnet 3.1 and i can not compile dotnet 5.0 on my ubuntu right now.
         }

         app.UseHangfireServer(options);
         if (env.EnvironmentName == "Development")
         {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
               c.SwaggerEndpoint("/swagger/v0.0.1/swagger.json", "Pavlov Rcon Webserver V0.0.1");
          
            });
            
         }
         app.UseStaticFiles();

         string connectionString = Configuration.GetConnectionString("DefaultConnection");


         if (env.EnvironmentName != "Development")
         {
            RecurringJob.AddOrUpdate(
               () => Steam.DeleteAllUnsedMapsFromAllServers(connectionString),
               Cron.Daily(3)); // Delete all unusedMaps every day on 3 in the morning
         }

         RecurringJob.AddOrUpdate( 
            () => Steam.CrawlSteamMaps(connectionString),
            Cron.Daily(2)); // Get all Maps every day on 2 in the morning

         if (env.EnvironmentName != "Development")
         {
            RecurringJob.AddOrUpdate( 
               () => RconStatic.CheckBansForAllServers(connectionString),
               string.Format("*/{0} * * * *", (object) 5)); // Check for bans and remove them is necessary
         }
         
         BackgroundJob.Schedule( 
            () => RconStatic.ReloadPlayerListFromServerAndTheServerInfo(connectionString,true),new TimeSpan(0,1,0)); // Check for bans and remove them is necessary
         
         BackgroundJob.Schedule( 
            () => SystemdService.CheckServiceStateForAll(connectionString),new TimeSpan(0,1,0)); // Check for bans and remove them is necessary


            
         app.UseRouting();
         app.UseAuthentication();
         app.UseAuthorization();
         app.UseHttpsRedirection();
         
         app.UseEndpoints(endpoints =>
         {
            endpoints.MapDefaultControllerRoute();
         });
         
      }
   }

}
