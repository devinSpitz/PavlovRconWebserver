using System.Collections.Generic;
using System.ComponentModel;

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
                  InputValue = false,
                  MinRole = "Mod"
                },
                new Command()
                {
                    Name  = "Kick",
                    InputValue = false,
                    MinRole = "Mod"
                },
                new Command()
                {
                    Name  = "InspectPlayer",
                    InputValue = false,
                    MinRole = "User"
                },
                new Command()
                {
                    Name  = "SwitchTeam",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "0", "1"
                    },
                    MinRole = "Captain"
                },
                new Command()
                {
                    Name  = "GiveItem",
                    InputValue = true,
                    PartialViewName = "ItemView",
                    MinRole = "Admin"
                    
                },
                new Command()
                {
                    Name  = "SetPlayerSkin",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "clown", "prisoner", "naked", "farmer", "russian", "nato"
                    },
                    MinRole = "Admin"
                },
                new Command()
                {
                    Name  = "GiveCash",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "500", "1000", "1500", "2000", "5000", "10000", "20000"
                    },
                    MinRole = "Admin"
                },
                new Command()
                {
                    Name  = "SetLimitedAmmoType",
                    InputValue = true,
                    valuesOptions = new List<string>()
                    {
                        "0","1","2"
                    },
                    MinRole = "Admin"
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
                    valuesTwoOptions = new List<string>()
                    {
                        "SND", "TDM", "DM", "GUN"
                    },
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
    }
    public class PlayerModel
    {
        public string Username { get; set; }
        public string UniqueId { get; set; }
    }
    public class Command
    {
        public string Name { get; set; }
        
        [DisplayName("Amoint/ItemId/etc.")]
        public bool InputValue { get; set; }

        public List<string> valuesOptions { get; set; } = new List<string>();
        public string PartialViewName { get; set; }
        
        public string MinRole { get; set; }
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