using AspNetCoreHero.ToastNotification;
using Hangfire;
using Hangfire.MemoryStorage;
using LiteDB.Identity.Async.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Services;
using Serilog;

namespace PavlovRconWebserver
{
    public static class CustomRoles
    {
        public const string Admin = "Admin";
        public const string Mod = "Admin,Mod";
        public const string Captain = "Captain,Admin,Mod";
        public const string User = "User,Captain,Mod,Admin,OnPremise,ServerRent";
        public const string OnPremise = "OnPremise,Admin";
        public const string OnPremiseOrRent = "OnPremise,ServerRent,Admin";
        public const string ServerRent = "ServerRent,Admin";
        public const string AnyOtherThanUser = "Captain,Mod,Admin,OnPremise,ServerRent";
    }

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
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
            services.AddHangfire(x => x.UseMemoryStorage());
            services.AddHangfireServer(x => { x.WorkerCount = 10; });

            GlobalConfiguration.Configuration.UseMemoryStorage();
            // JobStorage.Current = new MemoryStorage();
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var steamKey = Configuration.GetConnectionString("SteamApiKey");
            services.AddLiteDbIdentityAsync(connectionString).AddDefaultTokenProviders();
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
            services.AddScoped<LogService>();
            services.AddScoped<SteamIdentityStatsServerService>();
            services.AddSingleton(Configuration);
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddAuthentication(options => { /* Authentication options */ })
            .AddSteam(options =>
            {
                options.ApplicationKey = steamKey;
            });

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
            services.AddToastify(config =>
            {
                config.DurationInSeconds = 20;
                config.Position = Position.Right;
                config.Gravity = Gravity.Top;
            });
        }
        public static bool DisallowsSameSiteNone(string userAgent)
        {
            // Check if a null or empty string has been passed in, since this
            // will cause further interrogation of the useragent to fail.
            if (string.IsNullOrWhiteSpace(userAgent))
                return false;
    
            // Cover all iOS based browsers here. This includes:
            // - Safari on iOS 12 for iPhone, iPod Touch, iPad
            // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
            // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
            // All of which are broken by SameSite=None, because they use the iOS networking
            // stack.
            if (userAgent.Contains("CPU iPhone OS 12") ||
                userAgent.Contains("iPad; CPU OS 12"))
            {
                return true;
            }

            // Cover Mac OS X based browsers that use the Mac OS networking stack. 
            // This includes:
            // - Safari on Mac OS X.
            // This does not include:
            // - Chrome on Mac OS X
            // Because they do not use the Mac OS networking stack.
            if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                userAgent.Contains("Version/") && userAgent.Contains("Safari"))
            {
                return true;
            }

            // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
            // and none in this range require it.
            // Note: this covers some pre-Chromium Edge versions, 
            // but pre-Chromium Edge does not require SameSite=None.
            if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            {
                return true;
            }

            return false;
        }
        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || env.EnvironmentName == "Test")
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");
            
            app.UseSerilogRequestLogging();
            //Todo handle when you wnat something else than subodmains xD and aslo if add add this javascript will still be broken so adjust there as well
            var subPath = Configuration.GetSection("SubPath");
            //Todo for next release figure out why arch ich failing
            //app.UsePathBase(subPath.Value);
            // app.Use((context, next) =>
            // {
            //     context.Request.PathBase = new PathString(subPath.Value);
            //     return next();
            // });
            if (env.EnvironmentName != "Test")
                if (env.EnvironmentName == "Development")
                {
                    // Enable middleware to serve generated Swagger as a JSON endpoint.
                    app.UseSwagger();

                    // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                    // specifying the Swagger JSON endpoint.
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v0.0.1/swagger.json", "Pavlov Rcon Webserver V0.0.3");
                    });
                }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirection();
            app.UseHangfireDashboard(
                "/hangfire"
                ,
                new DashboardOptions
                {
                    Authorization = new[] {new HangfireAuthorizeFilter()}
                }
            );

            

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
            if (env.EnvironmentName != "Test")
                using (var serviceScope =
                    app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var steamService = serviceScope.ServiceProvider.GetService<SteamService>();
                    var rconService = serviceScope.ServiceProvider.GetService<RconService>();
                    var pavlovServerService = serviceScope.ServiceProvider.GetService<PavlovServerService>();
                    var userService = serviceScope.ServiceProvider.GetService<UserService>();
                    var matchService = serviceScope.ServiceProvider.GetService<MatchService>();
                    userService?.CreateDefaultRoles().GetAwaiter().GetResult();
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
                        Cron.Daily(3)); // Check server states
                    
                    RecurringJob.AddOrUpdate(
                        () => steamService.CrawlOculusMaps(),
                        "*/5 * * * *"); // Check server states

                    RecurringJob.AddOrUpdate(
                        () => steamService.CrawlSteamProfile(),
                        Cron.Daily(4)); // Check server states

                    BackgroundJob.Enqueue(() => matchService.RestartAllTheInspectorsForTheMatchesThatAreOnGoing());
                    
                    RecurringJob.AddOrUpdate(
                        () => rconService.ReloadPlayerListFromServerAndTheServerInfo(),
                        Cron.Minutely()); // Check server states
                }
        }
    }
}