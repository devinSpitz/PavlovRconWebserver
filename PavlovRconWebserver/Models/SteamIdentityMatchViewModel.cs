using System.Collections.Generic;
using System.ComponentModel;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchViewModel
    {
        [DisplayName("Available Steam Identities")]
        public List<SteamIdentity> AllSteamIdentities { get; set; }

        [DisplayName("Selected Steam Identities")]
        public List<SteamIdentity> SelectedSteamIdentities { get; set; }
    }
}