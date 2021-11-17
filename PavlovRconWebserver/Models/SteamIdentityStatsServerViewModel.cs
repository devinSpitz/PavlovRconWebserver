using System;
using System.Collections.Generic;
using System.ComponentModel;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    
    public class LeaderBoardViewModel
    {
        
        [DisplayName("Select the server:")]
        public int server { get; set; }
        public List<PavlovServer> AllServers { get; set; }
        public IEnumerable<SteamIdentityStatsServerViewModel> list { get; set; }
        
    }
    public class SteamIdentityStatsServerViewModel : SteamIdentityStatsServer
    {
        public string serverName { get; set; } = "";
    }
}