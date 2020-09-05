using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class Team
    {
        public List<SteamIdentity> Players { get; set; }
        public string Name { get; set; }
    }
}