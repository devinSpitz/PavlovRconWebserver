using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class TeamSelectedSteamIdentitiesViewModel
    {
        public ICollection<TeamSelectedSteamIdentity> SelectedSteamIdentities { get; set; }
        public ICollection<SteamIdentity> AllSteamIdentities { get; set; }
        public int TeamId { get; set; }
    }
}