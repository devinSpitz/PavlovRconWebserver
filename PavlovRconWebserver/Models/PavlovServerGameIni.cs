using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AspNetCoreHero.ToastNotification.Abstractions;
using PavlovRconWebserver.Extensions;
using Serilog.Events;

namespace PavlovRconWebserver.Models
{
    // This is not meant to go in to the database.
    // Its reader bean read or writen as its needed
    // Right now i don't think this this will happen that often.
    // May whithin a tournament it would be usfull but i don't think so
    public enum LimitedAmmoTypeEnum
    {
        Unlimited = 0,
        LimitedGeneric = 1,
        LimitedSpecific = 2,
        Custom = 3,
        LimitedSpecial = 4,
        BoxlessMode = 5
    }
    public class PavlovServerGameIni
    {
        public int serverId { get; set; } = 0;
        public bool bEnabled { get; set; } = true;
        public string ServerName { get; set; } = "";
        public int MaxPlayers { get; set; } = 10;
        public bool bSecured { get; set; } = true;
        public bool bCustomServer { get; set; } = true;
        public bool bWhitelist { get; set; } = true;
        public int RefreshListTime { get; set; } = 120;
        public LimitedAmmoTypeEnum LimitedAmmoType { get; set; } = 0;
        public int TickRate { get; set; } = 90;
        public int TimeLimit { get; set; } = 60;
        public string Password { get; set; } = "";
        public string BalanceTableURL { get; set; } = "";

        public List<PavlovServerGameIniMap> MapRotation { get; set; } =
            new List<PavlovServerGameIniMap>(); // example string = (MapId="UGC1758245796", GameMode="GUN")

        public bool ReadFromFile(PavlovServer pavlovServer,IToastifyService notyfService)
        {
            var gameIniContent = RconStatic.GetFile(pavlovServer,
                pavlovServer.ServerFolderPath + FilePaths.GameIni,notyfService);
            var lines = gameIniContent.Split("\n");
            var first = true; // cause the first line is to ignore
            foreach (var line in lines)
            {
                var tmpLine = line.CutLineAfter('#');

                if (first)
                {
                    first = false;
                    continue;
                }

                tmpLine = tmpLine.Replace(Environment.NewLine, "").Replace("\r", "").Replace("\n", "");
                //Bools
                if (tmpLine.Contains("bEnabled="))
                {
                    var tmp = tmpLine.Replace("bEnabled=", "");
                    bEnabled = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bEnabled
                    };
                }
                else if (tmpLine.Contains("bSecured="))
                {
                    var tmp = tmpLine.Replace("bSecured=", "");
                    bSecured = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bSecured
                    };
                }
                else if (tmpLine.Contains("bCustomServer="))
                {
                    var tmp = tmpLine.Replace("bCustomServer=", "");
                    bCustomServer = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bCustomServer
                    };
                }
                else if (tmpLine.Contains("bWhitelist="))
                {
                    var tmp = tmpLine.Replace("bWhitelist=", "");
                    bWhitelist = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bWhitelist
                    };
                }
                //String
                else if (tmpLine.Contains("ServerName="))
                {
                    ServerName = tmpLine.Replace("ServerName=", "");
                }
                else if (tmpLine.Contains("Password="))
                {
                    Password = tmpLine.Replace("Password=", "");
                }
                else if (tmpLine.Contains("BalanceTableURL="))
                {
                    BalanceTableURL = tmpLine.Replace("BalanceTableURL=", "");
                }
                //ints
                else if (tmpLine.Contains("MaxPlayers="))
                {
                    var tmp = tmpLine.Replace("MaxPlayers=", "");
                    MaxPlayers = int.Parse((string) tmp);
                }
                else if (tmpLine.Contains("RefreshListTime="))
                {
                    var tmp = tmpLine.Replace("RefreshListTime=", "");
                    RefreshListTime = int.Parse((string) tmp);
                }
                else if (tmpLine.Contains("LimitedAmmoType="))
                {
                    var tmp = tmpLine.Replace("LimitedAmmoType=", "");
                    var numeric = int.Parse((string) tmp);
                    LimitedAmmoType = numeric switch
                    {
                        (int) LimitedAmmoTypeEnum.Unlimited => LimitedAmmoTypeEnum.Unlimited,
                        (int) LimitedAmmoTypeEnum.LimitedGeneric => LimitedAmmoTypeEnum.LimitedGeneric,
                        (int) LimitedAmmoTypeEnum.LimitedSpecific => LimitedAmmoTypeEnum.LimitedSpecific,
                        (int) LimitedAmmoTypeEnum.Custom => LimitedAmmoTypeEnum.Custom,
                        (int) LimitedAmmoTypeEnum.LimitedSpecial => LimitedAmmoTypeEnum.LimitedSpecial,
                        (int) LimitedAmmoTypeEnum.BoxlessMode => LimitedAmmoTypeEnum.BoxlessMode,
                        _ => LimitedAmmoType
                    };
                }
                else if (tmpLine.Contains("TickRate="))
                {
                    var tmp = line.Replace("TickRate=", "");
                    TickRate = int.Parse((string) tmp);
                }
                else if (tmpLine.Contains("TimeLimit="))
                {
                    var tmp = tmpLine.Replace("TimeLimit=", "");
                    TimeLimit = int.Parse(tmp);
                }
            }

            return true;
        }


        public string SaveToFile(PavlovServer pavlovServer, ServerSelectedMap[] serverSelectedMaps, IToastifyService notyfService)
        {
            var lines = new List<string>();
            lines.Add("[/Script/Pavlov.DedicatedServer]");
            lines.Add("bEnabled=" + bEnabled.ToString().ToLower());
            lines.Add("ServerName=" + ServerName);
            lines.Add("MaxPlayers=" + MaxPlayers);
            lines.Add("bSecured=" + bSecured.ToString().ToLower());
            lines.Add("bCustomServer=" + bCustomServer.ToString().ToLower());
            lines.Add("bWhitelist=" + bWhitelist.ToString().ToLower());
            lines.Add("RefreshListTime=" + RefreshListTime);
            lines.Add("LimitedAmmoType=" + (int)LimitedAmmoType);
            lines.Add("TickRate=" + TickRate);
            lines.Add("TimeLimit=" + TimeLimit);
            if (!string.IsNullOrEmpty(Password))
                lines.Add("Password=" + Password);
            else
                lines.Add("Password=");

            if (!string.IsNullOrEmpty(BalanceTableURL))
                lines.Add("BalanceTableURL=" + BalanceTableURL);
            else
                lines.Add("BalanceTableURL=");

            foreach (var serverSelectedMap in serverSelectedMaps)
                if (Regex.IsMatch(serverSelectedMap.Map.Id, @"^\d+$"))
                    lines.Add("MapRotation=(MapId=\"UGC" + serverSelectedMap.Map.Id + "\", GameMode=\"" +
                              serverSelectedMap.GameMode + "\")");
                else
                    lines.Add("MapRotation=(MapId=\"" + serverSelectedMap.Map.Id + "\", GameMode=\"" +
                              serverSelectedMap.GameMode + "\")");
            var content = string.Join(Environment.NewLine, lines);
            
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("prepared game ini",LogEventLevel.Verbose,notyfService);
            var result = RconStatic.WriteFile(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.GameIni,
                content,notyfService);
            
            DataBaseLogger.LogToDatabaseAndResultPlusNotify("saved game ini",LogEventLevel.Verbose,notyfService);
            return result;
        }
    }
}