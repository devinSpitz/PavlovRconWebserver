using System;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace PavlovRconWebserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--version")
            {
                Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
                Environment.Exit(0);
            }
            var configSettings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var logLevel = LogEventLevel.Warning;
            var logTime = new TimeSpan(1, 0, 0, 0);
            // if development than show all logs in the console otherwise only warning and above
            if (environment == Environments.Development)
            {
                logLevel = LogEventLevel.Verbose;
                logTime = new TimeSpan(0, 0, 30, 0);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .ReadFrom.Configuration(configSettings)
                .WriteTo.Console(logLevel)
                .WriteTo.LiteDbAsync(configSettings.GetConnectionString("DefaultConnection"), logLevel,
                    logTime)
                .CreateBootstrapLogger();


            Log.Information("Starting up!");

            try
            {
                CreateHostBuilder(args).Build().Run();

                Log.Information("Stopped cleanly");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}