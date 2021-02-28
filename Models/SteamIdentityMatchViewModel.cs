using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchViewModel
    {
        public List<SteamIdentity> AllSteamIdentities { get; set; }
        public List<SteamIdentity> SelectedSteamIdentities { get; set; }
        
    }
}