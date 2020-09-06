using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class TeamSelectedSteamIdentitiesViewModel
    {
        public List<TeamSelectedSteamIdentity> SelectedSteamIdentities { get; set; }
        public List<SteamIdentity> AllSteamIdentities { get; set; }
        public int TeamId { get; set; }
    }
}