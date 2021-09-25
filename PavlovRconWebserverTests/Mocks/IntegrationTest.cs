using System;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PavlovRconWebserver;
using PavlovRconWebserverTests.UnitTests;

namespace PavlovRconWebserverTests.Mocks
{

    /// <summary>
    /// WIP
    /// </summary>
    
    public static class IntegrationTest
    {
        public static TestServer _server;
        public static TestServer RunTestHost()
        {
            if (_server != null) return _server;
            var applicationPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../PavlovRconWebserver/"));
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    // Add TestServer
                    webHost.UseTestServer();
                    webHost.UseEnvironment("Test");
                    webHost.UseContentRoot(applicationPath);
                    //webHost.UseSetting(WebHostDefaults.ApplicationKey, typeof(Startup).Assembly.GetName().Name);
                    webHost.UseStartup<TestStartup>();
                    webHost.ConfigureAppConfiguration((hostContext, config) =>
                    {
                        var integrationConfig = new ConfigurationBuilder()
                            .AddJsonFile(Path.GetFullPath(Path.Combine("appsettings.json")), optional: false)
                            .Build();
                        config.AddConfiguration(integrationConfig);
                    });
                    webHost.ConfigureServices(services =>
                    {
                        services.AddAuthentication("Test")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                "Test", options => { });
                    });
                    webHost.ConfigureTestServices(services =>
                    {
                        // services.AddAuthentication("Test")
                        //     .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        //         "Test", options => { });
                        services.AddMvc()
                            .AddApplicationPart(typeof(Startup).Assembly);
                        services.AddRazorPages(options =>
                        {
                            options.Conventions.AuthorizePage("/Account/Login");
                        });
                    });
                    
                });
            var host = hostBuilder.Start();
            _server = host.GetTestServer();

            return _server;
        }
        //
        // public static Task<AuthenticateResult> MockUsers(string role)
        // {
        //
        //     AuthenticationTicket ticket = null;
        //     if (role == "Admin")
        //     {
        //         var claims = new[]
        //         {
        //             new Claim(ClaimTypes.Name, "test"),
        //             new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        //             new Claim(ClaimTypes.Role, "Admin"),
        //
        //         };
        //         var identity = new ClaimsIdentity(claims, "Admin");
        //         var principal = new ClaimsPrincipal(identity);
        //         ticket = new AuthenticationTicket(principal, "Test");
        //     }
        //     else if (role == "Mod")
        //     {
        //         var claims = new[]
        //         {
        //             new Claim(ClaimTypes.Name, "test"),
        //             new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        //             new Claim(ClaimTypes.Role, "Mod"),
        //         };
        //         var identity = new ClaimsIdentity(claims, "Mod");
        //         var principal = new ClaimsPrincipal(identity);
        //         ticket = new AuthenticationTicket(principal, "Test");
        //     }
        //     else if (role == "Captain")
        //     {
        //         var claims = new[]
        //         {
        //             new Claim(ClaimTypes.Name, "test"),
        //             new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        //             new Claim(ClaimTypes.Role, "Captain"),
        //
        //         };
        //         var identity = new ClaimsIdentity(claims, "Captain");
        //         var principal = new ClaimsPrincipal(identity);
        //         ticket = new AuthenticationTicket(principal, "Test");
        //
        //     }
        //     else if (role == "User")
        //     {
        //         var claims = new[]
        //         {
        //             new Claim(ClaimTypes.Name, "test"),
        //             new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        //             new Claim(ClaimTypes.Role, "Captain"),
        //         };
        //         var identity = new ClaimsIdentity(claims, "User");
        //         var principal = new ClaimsPrincipal(identity);
        //         ticket = new AuthenticationTicket(principal, "Test");
        //
        //     }
        //
        //     var result = AuthenticateResult.Success(ticket);
        //
        //     return Task.FromResult(result);
        // }
    }
}