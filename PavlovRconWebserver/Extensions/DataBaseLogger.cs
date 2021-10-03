using System;
using System.Reflection;
using AspNetCoreHero.ToastNotification.Abstractions;
using PavlovRconWebserver.Models;
using Serilog;
using Serilog.Events;

namespace PavlovRconWebserver.Extensions
{
    public static class DataBaseLogger
    {
        public static readonly string ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public static void LogToDatabaseAndResultPlusNotify(string error,LogEventLevel logLevel,IToastifyService notyfService, ConnectionResult result = null)
        {
            result?.errors.Add(error);
            switch (logLevel)
            {
                case LogEventLevel.Verbose:
                    Log.Verbose(error+" | Version={ProgramVersion} |",ProgramVersion); 
                    break;
                case LogEventLevel.Debug:
                    Log.Debug(error+" | Version={ProgramVersion} |",ProgramVersion); 
                    break;
                case LogEventLevel.Information:
                    Log.Information(error+" | Version={ProgramVersion} |",ProgramVersion); 
                    break;
                case LogEventLevel.Warning:
                    Log.Warning(error+" | Version={ProgramVersion} |",ProgramVersion);
                    notyfService.Warning(error,10);
                    break;
                case LogEventLevel.Error:
                    Log.Error(error+" | Version={ProgramVersion} |",ProgramVersion); 
                    notyfService.Error(error,60);
                    break;
                case LogEventLevel.Fatal:
                    Log.Fatal(error+" | Version={ProgramVersion} |",ProgramVersion); 
                    notyfService.Error(error,60);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }
    }
}