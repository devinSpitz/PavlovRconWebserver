using System;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using LiteDB.Async;
using LiteDB.Identity.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddHangfire(x => x.UseMemoryStorage());
            services.AddHangfireServer(x =>
            {
                x.WorkerCount = 10;
            });
            JobStorage.Current = new MemoryStorage();
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddLiteDBIdentity(connectionString).AddDefaultTokenProviders();
            //services.AddLiteAsyncDBIdentity(connectionString).AddDefaultTokenProviders();
            
            var db = new LiteDatabaseAsync("Filename=mydatabase.db;Connection=shared;Password=hunter2");
            // Add LiteDB Dependency. Thare are three ways to set database:
            // 1. By default it uses the first connection string on appsettings.json, ConnectionStrings section.
            services.AddScoped<SshServerSerivce>();
            services.AddScoped<UserService>();
            services.AddScoped<RconService>();
            services.AddScoped<ServerSelectedMapService>();
            services.AddScoped<MapsService>();
            services.AddScoped<TeamService>();
            services.AddScoped<SteamIdentityService>();
            services.AddScoped<TeamSelectedSteamIdentityService>();
            services.AddScoped<MatchService>();
            services.AddScoped<PavlovServerService>();
            services.AddScoped<ServerBansService>();
            services.AddScoped<PavlovServerPlayerService>();
            services.AddScoped<PavlovServerInfoService>();
            services.AddScoped<PavlovServerPlayerHistoryService>();
            services.AddScoped<MatchSelectedSteamIdentitiesService>();
            services.AddScoped<MatchSelectedTeamSteamIdentitiesService>();
            services.AddScoped<ServerSelectedWhitelistService>();
            services.AddScoped<ServerSelectedModsService>();
            services.AddScoped<PublicViewListsService>();
            services.AddScoped<SteamService>();
            services.AddSingleton(Configuration);
            services.AddScoped<IEmailSender, EmailSender>();
            
            
            
            // services
            //    .AddAuthentication(cfg =>
            //    {
            //       cfg.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    })
            //    .AddCookie();
            // Add application services.
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v0.0.1", new OpenApiInfo
                {
                    Title = "Pavlov Rcon Webserver API",
                    Version = "v0.0.1",
                    Description = "Here you can see all function of the PavlovRconWebserver"
                });
            });
            services.AddMvc().AddRazorRuntimeCompilation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            if (env.EnvironmentName == "Development")
                app.UseHangfireDashboard(
                    "/hangfire"
                    //,
                    // new DashboardOptions
                    // {
                    //    Authorization = new IDashboardAuthorizationFilter[]
                    //    {
                    //       new DashboardAuthorizationFilter()
                    //    }
                    // }
                ); // Does not work on dotnet 3.1 and i can not compile dotnet 5.0 on my ubuntu right now.
            
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

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirection();
            
            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var steamService = serviceScope.ServiceProvider.GetService<SteamService>();
                var rconService = serviceScope.ServiceProvider.GetService<RconService>();
                var pavlovServerService = serviceScope.ServiceProvider.GetService<PavlovServerService>();
                if (env.EnvironmentName != "Development")
                {
                    
                    RecurringJob.AddOrUpdate(
                        () => steamService.DeleteAllUnsedMapsFromAllServers(),
                        Cron.Daily(3)); // Delete all unusedMaps every day on 3 in the morning

                    RecurringJob.AddOrUpdate(
                        () => rconService.CheckBansForAllServers(),
                        "*/5 * * * *"); // Check for bans and remove them is necessary

                    RecurringJob.AddOrUpdate(
                        () => pavlovServerService.CheckStateForAllServers(),
                        Cron.Minutely()); // Check server states
                }


                RecurringJob.AddOrUpdate(
                    () => steamService.CrawlSteamMaps(),
                    Cron.Daily(2)); // Get all Maps every day on 2 in the morning


                BackgroundJob.Schedule(
                    () => rconService.ReloadPlayerListFromServerAndTheServerInfo(true),
                    new TimeSpan(0, 1, 0)); // Check for bans and remove them is necessary
            }


        }
    }
}