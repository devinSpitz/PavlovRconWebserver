using System.Collections.Generic;
using System.ComponentModel;

namespace PavlovRconWebserver.Models
{
    public class RconViewModel
    {
        public RconViewModel()
        {
            PlayerCommands = new List<Command>()
            {
                new Command()
                {
                  Name  = "Ban",
                  InputValue = false
                },
                new Command()
                {
                    Name  = "Kick",
                    InputValue = false
                },
                new Command()
                {
                    Name  = "InspectPlayer",
                    InputValue = false
                },
                new Command()
                {
                    Name  = "SwitchTeam",
                    InputValue = true
                },
                new Command()
                {
                    Name  = "GiveItem",
                    InputValue = true
                },
                new Command()
                {
                    Name  = "SetPlayerSkin",
                    InputValue = true
                },
                new Command()
                {
                    Name  = "GiveCash",
                    InputValue = true
                }
            };
        }
        
        [DisplayName("Select the servers you wana execute the command:")]
        public List<RconServer> RconServer { get; set; }
        public string Command { get; set; }
        public List<PlayerModel> Players { get; set; } = new List<PlayerModel>();
        public List<PlayerModel> PlayersSelected { get; set; } = new List<PlayerModel>();

        public List<Command> PlayerCommands { get; }
        
        [DisplayName("Value")]
        public string PlayerValue { get; set; }
    }

    public class PlayerListClass
    {
        public List<PlayerModel> PlayerList { get; set; }
    }
    public class PlayerModel
    {
        public string Name { get; set; }
        public string SteamId { get; set; }
    }
    public class Command
    {
        public string Name { get; set; }
        public bool InputValue { get; set; }
    }
}