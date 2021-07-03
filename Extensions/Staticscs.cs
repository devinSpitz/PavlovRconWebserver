using System;
using System.Collections;
using System.Collections.Generic;

namespace PavlovRconWebserver.Extensions
{
    public static class Statics
    {
        public static IDictionary<string, TimeSpan> BanList { get; } = new Dictionary<string, TimeSpan>()
        {
            {"unlimited", new TimeSpan(9999999, 0, 0, 0, 0)},
            {"5min", new TimeSpan(0, 0, 5, 0, 0)},
            {"10min", new TimeSpan(0, 0, 10, 0, 0)},
            {"30min", new TimeSpan(0, 0, 30, 0, 0)},
            {"1h", new TimeSpan(0, 1, 0, 0, 0)},
            {"3h", new TimeSpan(0, 3, 0, 0, 0)},
            {"6h", new TimeSpan(0, 6, 0, 0, 0)},
            {"12h", new TimeSpan(0, 12, 0, 0, 0)},
            {"24h", new TimeSpan(0, 24, 0, 0, 0)},
            {"48h", new TimeSpan(2, 0, 0, 0, 0)},
        };


        
        public static string[] Models = new[]
        {
            "none","clown", "prisoner", "naked", "farmer", "russian", "nato", "us", "soviet", "german"
        };
        
    }

    public static class GameModes
    {
        public static IDictionary<string, bool> HasTeams = new Dictionary<string, bool>()
        {
            {"SND",true},
            {"TDM",true},
            {"DM",false},
            {"GUN",false},
            {"ZWV",true},
            {"WW2GUN",true},
            {"TANKTDM",true},
            {"KOTH",false},
        };
        
        public static IDictionary<string, bool> OneTeam = new Dictionary<string, bool>()
        {
            {"SND",false},
            {"TDM",false},
            {"DM",false},
            {"GUN",false},
            {"ZWV",true},
            {"WW2GUN",false},
            {"TANKTDM",false},
            {"KOTH",false},
        };

        public static string[] ModesString = new[]
        {
            "SND", "TDM", "DM", "GUN","ZWV", "WW2GUN", "TANKTDM", "KOTH"
        };        
        

    }
}