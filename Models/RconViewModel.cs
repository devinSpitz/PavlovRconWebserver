using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class RconViewModel
    {
        //ToDO: Need to save Baned players to make Unban ane usfull cause will will be resetted if you restart your server when you set this option via rcon!
        public RconViewModel()
        {
            SpecialCommands = new List<Command>()
            {
                new Command()
                {
                    Name  = "ServerInfo",
                    InputValue = false,
                    MinRole = "User"
                },
                new Command()
                {
                    Name  = "RefreshList",
                    InputValue = false,
                    MinRole = "User"
                },
                new Command()
                {
                    Name  = "ResetSND",
                    InputValue = false,
                    MinRole = "Captain"
                },
                new Command()
                {
                    Name  = "Blacklist",
                    InputValue = false,
                    MinRole = "Captain"
                },
                new Command()
                {
                    Name  = "RotateMap",
                    InputValue = false,
                    MinRole = "Captain"
                }

            };
            PlayerCommands = new List<Command>()
            {
                new Command()
                {
                  Name  = "Ban",
                  InputValue = true,
                  MinRole = "Mod",
                  Group = "Player commands",
                  valuesOptions = new List<string>()
                  {
                    "unlimited", "5min", "10min", "30min", "1h", "3h", "6h", "12h", "24h", "48h"
                  },
                },new Command()
                {
                    Name  = "Unban",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },new Command()
                {
                    Name  = "Kill",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "Kick",
                    InputValue = false,
                    MinRole = "Mod",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "InspectPlayer",
                    InputValue = false,
                    MinRole = "User",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "SwitchTeam",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "0", "1"
                    },
                    MinRole = "Captain",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "GiveItem",
                    InputValue = true,
                    PartialViewName = "ItemView",
                    MinRole = "Admin",
                    Group = "Player commands"
                    
                },
                new Command()
                {
                    Name  = "SetPlayerSkin",
                    InputValue = true,
                    valuesOptions = Statics.Models.ToList(),
                    MinRole = "Admin",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "GiveCash",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "500", "1000", "1500", "2000", "5000", "10000", "20000"
                    },
                    MinRole = "Admin",
                    Group = "Player commands"
                },
                new Command()
                {
                    Name  = "SetLimitedAmmoType",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "0","1","2"
                    },
                    MinRole = "Admin",
                    Group = "Server commands"
                }
            };
            TwoValueCommands = new List<ExtendedCommand>()
            {
                new ExtendedCommand()
                {
                    Name  = "GiveTeamCash",
                    InputValue = true,
                    InputValueTwo = true,
                    valuesOptions = new List<string>()
                    {
                        "0", "1"
                    },
                    valuesTwoOptions = new List<string>()
                    {
                        "500", "1000", "1500", "2000", "5000", "10000", "20000"
                    },
                    MinRole = "Admin"
                },
                new ExtendedCommand()
                {
                    Name  = "SwitchMap",
                    InputValue = true,
                    InputValueTwo = true,
                    valuesTwoOptions = GameModes.ModesString.ToList(),
                    PartialViewName = "https://steamcommunity.com/workshop/browse/?appid=555160&browsesort=trend&section=readytouseitems&actualsort=trend&p=1&numperpage=30",
                    MinRole = "Captain"
                }
            };
        }
        
        [DisplayName("Select the server you wanna execute the commands:")]
        public List<PavlovServer> SingleServer { get; set; }
        public string Command { get; set; }
        public bool MultiRcon = false;
        public List<PlayerModel> Players { get; set; } = new List<PlayerModel>();
        public List<PlayerModel> PlayersSelected { get; set; } = new List<PlayerModel>();

        [DisplayName("Server/Player commands")]
        public List<Command> PlayerCommands { get; } = new List<Command>();
        public List<Command> SpecialCommands { get; } = new List<Command>();
        public List<ExtendedCommand> TwoValueCommands { get; } = new List<ExtendedCommand>();
        
        [DisplayName("Value")]
        public string PlayerValue { get; set; }
        
        [DisplayName("SecondValue")]
        public string PlayerValueTwo { get; set; }
    }

    public class PlayerListClass
    {
        public List<PlayerModel> PlayerList { get; set; }
        public List<PlayerModelExtended> PlayerListExtended { get; set; } = new List<PlayerModelExtended>();
    }
    public class PlayerModel
    {
        public string Username { get; set; }
        public string UniqueId { get; set; }
    }

    public class PlayerModelExtendedRconModel
    {
        public PlayerModelExtended PlayerInfo = new PlayerModelExtended();
    }
    public class PlayerModelExtended : PlayerModel
    {
        public string PlayerName { get; set; } = "";
        public string KDA { get; set; } = "";
        public string Cash { get; set; } = "";
        public int TeamId { get; set; } = 0;
        public int Score { get; set; } = 0;

        public string getKills()
        {
            var tmp = KDA?.Split("/");
            if (tmp?.Length != 3) return "0";
            return tmp[0];
        }
        public string getDeaths()
        {
            var tmp = KDA?.Split("/");
            if (tmp?.Length != 3) return "0";
            return tmp[1];
        }
        public string getAssists()
        {
            var tmp = KDA?.Split("/");;
            if (tmp?.Length != 3) return "0";
            return tmp[2];
        }
        
    }
    
    public class Command
    {
        public string Name { get; set; }
        
        [DisplayName("Amoint/ItemId/etc.")]
        public bool InputValue { get; set; }

        public List<string> valuesOptions { get; set; } = new List<string>();
        public string PartialViewName { get; set; }
        
        public string MinRole { get; set; }

        public string Group { get; set; }
    }
    
    public class ExtendedCommand: Command
    {
        [DisplayName("Team/MapId")]
        public new bool InputValue { get; set; }
        
        [DisplayName("Amount/GameMode")]
        public bool InputValueTwo { get; set; }
        
        public List<string> valuesTwoOptions { get; set; } = new List<string>();
    }
}