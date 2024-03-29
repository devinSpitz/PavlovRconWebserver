using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class RconViewModel
    {
        public RconViewModel()
        {
            SpecialCommands = new List<Command>
            {
                new()
                {
                    Name = "ServerInfo",
                    InputValue = false,
                    MinRole = "User"
                },
                new()
                {
                    Name = "RefreshList",
                    InputValue = false,
                    MinRole = "User"
                },
                new()
                {
                    Name = "ResetSND",
                    InputValue = false,
                    MinRole = "Captain"
                },
                new()
                {
                    Name = "Blacklist",
                    InputValue = false,
                    MinRole = "Captain"
                },
                new()
                {
                    Name = "RotateMap",
                    InputValue = false,
                    MinRole = "Captain"
                }
            };
            PlayerCommands = new List<Command>
            {
                new()
                {
                    Name = "Ban",
                    InputValue = true,
                    MinRole = "Mod",
                    Group = "Player commands",
                    valuesOptions = new List<string>
                    {
                        "unlimited", "5min", "10min", "30min", "1h", "3h", "6h", "12h", "24h", "48h"
                    }
                },
                new()
                {
                    Name = "Unban",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "Kill",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "Kick",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "AddMod",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "RemoveMod",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "InspectPlayer",
                    InputValue = false,
                    MinRole = "User",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "tttflushkarma",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "SwitchTeam",
                    InputValue = true,
                    valuesOptions = new List<string>
                    {
                        "0", "1"
                    },
                    MinRole = "Captain",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "Slap",
                    InputValue = true,
                    valuesOptions = new List<string>
                    {
                        "0", "20","40","60","80","100"
                    },
                    MinRole = "Captain",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "tttsetkarma",
                    InputValue = true,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "GiveItem",
                    InputValue = true,
                    PartialViewName = "ItemView",
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "GodMode",
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "CustomPlayer",
                    InputValue = true,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "GiveVehicle",
                    InputValue = true,
                    PartialViewName = "ItemView",
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "SetPlayerSkin",
                    InputValue = true,
                    valuesOptions = Statics.Models.ToList(),
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "GiveCash",
                    InputValue = true,
                    valuesOptions = new List<string>
                    {
                        "500", "1000", "1500", "2000", "5000", "10000", "20000"
                    },
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new()
                {
                    Name = "SetLimitedAmmoType",
                    InputValue = true,
                    valuesOptions = new List<string>
                    {
                        "0", "1", "2", "3", "4", "5"
                    },
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "SetPin",
                    InputValue = true,
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "ItemList",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "Shownametags",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "tttendround",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "tttpausetimer",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Server commands"
                },
                new()
                {
                    Name = "TTTAlwaysEnableSkinMenu",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Server commands"
                }
            };
            TwoValueCommands = new List<ExtendedCommand>
            {
                new()
                {
                    Name = "GiveTeamCash",
                    InputValue = true,
                    InputValueTwo = true,
                    valuesOptions = new List<string>
                    {
                        "0", "1"
                    },
                    valuesTwoOptions = new List<string>
                    {
                        "500", "1000", "1500", "2000", "5000", "10000", "20000"
                    },
                    MinRole = "Mod"
                },
                new()
                {
                    Name = "SwitchMap",
                    InputValue = true,
                    InputValueTwo = true,
                    valuesTwoOptions = GameModes.ModesString.ToList(),
                    PartialViewName =
                        "https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p=1&numperpage=30",
                    MinRole = "Captain"
                },
                new()
                {
                    Name = "Custom",
                    InputValue = true,
                    InputValueTwo = true,
                    MinRole = "Mod"
                }
            };
        }

        [DisplayName("Select the server you wanna execute the commands:")]
        public List<PavlovServer> SingleServer { get; set; }

        public string Command { get; set; }
        public List<PlayerModel> Players { get; set; } = new();
        public List<PlayerModel> PlayersSelected { get; set; } = new();

        [DisplayName("Server/Player commands")]
        public List<Command> PlayerCommands { get; } = new();

        public List<Command> SpecialCommands { get; } = new();
        public List<ExtendedCommand> TwoValueCommands { get; } = new();

        [DisplayName("Value")] public string PlayerValue { get; set; }

        [DisplayName("SecondValue")] public string PlayerValueTwo { get; set; }
        [DisplayName("All")] public bool CheckAll { get; set; } = false;
    }

    public class EndStatsFromLogs
    {
        public List<PlayerModelEndStatsFromLogs> allStats { get; set; }
    }
    
    public class PlayerModelEndStatsFromLogs
    {
        public string uniqueId { get; set; }
        public List<StatsObjectEndStatsFromLogs>  stats { get; set; }
    }
    
    public class StatsObjectEndStatsFromLogs
    {
        public string statType { get; set; }
        public int amount { get; set; }
        
    }
    
    public class PlayerListClass
    {
        public List<PlayerModel> PlayerList { get; set; }
        public List<PlayerModelExtended> PlayerListExtended { get; set; } = new();
    }

    public class PlayerModel
    {
        public string Username { get; set; }
        public string UniqueId { get; set; }
    }

    public class PlayerModelExtendedRconModel
    {
        public PlayerModelExtended PlayerInfo = new();
    }

    public class PlayerModelExtended : PlayerModel
    {
        public string PlayerName { get; set; } = "";
        public string KDA { get; set; } = "";
        public string Cash { get; set; } = "";
        public int TeamId { get; set; } = 0;
        public int Score { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Assists { get; set; } = 0;

        public string getKills()
        {
            if (Kills != 0) return Kills.ToString();
            var tmp = KDA?.Split("/");
            if (tmp?.Length != 3) return "0";
            return tmp[0];
        }

        public string getDeaths()
        {
            if (Deaths != 0) return Deaths.ToString();
            var tmp = KDA?.Split("/");
            if (tmp?.Length != 3) return "0";
            return tmp[1];
        }

        public string getAssists()
        {
            if (Assists != 0) return Assists.ToString();
            var tmp = KDA?.Split("/");
            if (tmp?.Length != 3) return "0";
            return tmp[2];
        }
    }

    public class Command
    {
        public string Name { get; set; }

        [DisplayName("Amoint/ItemId/etc.")] public bool InputValue { get; set; }
        
        public List<string> valuesOptions { get; set; } = new();
        public string PartialViewName { get; set; }

        public string MinRole { get; set; }

        public string Group { get; set; }
    }

    public class ExtendedCommand : Command
    {
        [DisplayName("Team/MapId")] public new bool InputValue { get; set; }

        [DisplayName("Amount/GameMode")] public bool InputValueTwo { get; set; }

        public List<string> valuesTwoOptions { get; set; } = new();
    }
}