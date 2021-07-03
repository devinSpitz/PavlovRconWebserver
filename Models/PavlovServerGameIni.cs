using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Models
{
    // This is not meant to go in to the database.
    // Its reader bean read or writen as its needed
    // Right now i don't think this this will happen that often.
    // May whithin a tournament it would be usfull but i don't think so
    
    public class PavlovServerGameIni
    {
        public int serverId { get; set; } = 0;
        public bool bEnabled { get; set; }  = true;
        public string ServerName { get; set; }   = "";
        public int MaxPlayers { get; set; }   = 10;
        public bool bSecured  { get; set; }  = true;
        public bool bCustomServer { get; set; }   = true;
        public bool bWhitelist { get; set; }   = true;
        public int RefreshListTime { get; set; }   = 120;
        public int LimitedAmmoType { get; set; }   = 0;
        public int TickRate { get; set; }   = 90;
        public int TimeLimit { get; set; }   = 60;
        public string Password { get; set; }   = "";
        public string BalanceTableURL { get; set; }   = "";
        public List<PavlovServerGameIniMap> MapRotation { get; set; }   = new List<PavlovServerGameIniMap>(); // example string = (MapId="UGC1758245796", GameMode="GUN")
        
        public async Task<bool> ReadFromFile(PavlovServer pavlovServer,RconService rconService)
        {
            var GameIniContent = await rconService.SendCommand(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.GameIni, false, true);
            var lines = GameIniContent.Split("\n");
            var first = true; // cause the first line is to ignore
            foreach (var line in lines)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                //Bools
                if (line.Contains("bEnabled="))
                {
                    var tmp = line.Replace("bEnabled=", "");
                    bEnabled = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bEnabled
                    };
                }
                else if (line.Contains("bSecured="))
                {
                    var tmp = line.Replace("bSecured=", "");
                    bSecured = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bSecured
                    };
                }
                else if (line.Contains("bCustomServer="))
                {
                    var tmp = line.Replace("bCustomServer=", "");
                    bCustomServer = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bCustomServer
                    };
                }
                else if (line.Contains("bWhitelist="))
                {
                    var tmp = line.Replace("bWhitelist=", "");
                    bWhitelist = tmp switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => bWhitelist
                    };
                }
                //String
                else if (line.Contains("ServerName="))
                {
                    ServerName = line.Replace("ServerName=", "");
                }
                else if (line.Contains("Password="))
                {
                    Password = line.Replace("Password=", "");
                }
                else if (line.Contains("BalanceTableURL="))
                {
                    BalanceTableURL = line.Replace("BalanceTableURL=", "");
                }
                else if (line.Contains("MapRotation="))
                {
                    var tmpPavlovServerGameIniMap = new PavlovServerGameIniMap();
                    
                    var tmp = line.Replace("MapRotation=(MapId=\"", "");
                    var indexToStartForGameMode = tmp.IndexOf(',');
                    tmpPavlovServerGameIniMap.MapLabel = tmp.Substring(0, indexToStartForGameMode);
                    var gameMode = tmp.Substring(indexToStartForGameMode);
                    gameMode = gameMode.Replace(" GameMode=\"", "");
                    gameMode = gameMode.Replace("\")", "");
                    tmpPavlovServerGameIniMap.GameMode = gameMode;
                    MapRotation.Add(tmpPavlovServerGameIniMap);
                }
                //ints
                else if (line.Contains("MaxPlayers="))
                {
                    var tmp = line.Replace("MaxPlayers=", "");
                    MaxPlayers = Int32.Parse(tmp);
                }
                else if (line.Contains("RefreshListTime="))
                {
                    var tmp = line.Replace("RefreshListTime=", "");
                    RefreshListTime = Int32.Parse(tmp);
                }
                else if (line.Contains("LimitedAmmoType="))
                {
                    var tmp = line.Replace("LimitedAmmoType=", "");
                    LimitedAmmoType = Int32.Parse(tmp);
                }
                else if (line.Contains("TickRate="))
                {
                    var tmp = line.Replace("TickRate=", "");
                    TickRate = Int32.Parse(tmp);
                }
                else if (line.Contains("TimeLimit="))
                {
                    var tmp = line.Replace("TimeLimit=", "");
                    TimeLimit = Int32.Parse(tmp);
                }
            }

            return true;
        }
        
        public async Task<bool> SaveToFile(PavlovServer pavlovServer,List<ServerSelectedMap> serverSelectedMaps,RconService rconService)
        {
            var lines = new List<string>();
            lines.Add("[/Script/Pavlov.DedicatedServer]");
            lines.Add("bEnabled="+bEnabled.ToString().ToLower());
            lines.Add("ServerName="+ServerName);
            lines.Add("MaxPlayers="+MaxPlayers);
            lines.Add("bSecured="+bSecured.ToString().ToLower());
            lines.Add("bCustomServer="+bCustomServer.ToString().ToLower());
            lines.Add("bWhitelist="+bWhitelist.ToString().ToLower());
            lines.Add("RefreshListTime="+RefreshListTime);
            lines.Add("LimitedAmmoType="+LimitedAmmoType);
            lines.Add("TickRate="+TickRate);
            lines.Add("TimeLimit="+TimeLimit);
            if(!string.IsNullOrEmpty(Password))
                lines.Add("Password="+Password);
            
            if(!string.IsNullOrEmpty(BalanceTableURL))
                lines.Add("BalanceTableURL="+BalanceTableURL);
            
            foreach (var serverSelectedMap in serverSelectedMaps)
            {
                lines.Add("MapRotation=(MapId=\""+serverSelectedMap.Map.Id+"\", GameMode=\""+serverSelectedMap.GameMode+"\")");
            }
            var content = string.Join(Environment.NewLine, lines);
            await rconService.SendCommand(pavlovServer, pavlovServer.ServerFolderPath + FilePaths.GameIni, false, false,
                content, true);
            return true;
        }
    }
}