using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchListViewModel
    {
        public string cssClass { get; set; }
        public List<SteamIdentity> SteamIdentities { get; set; }
        
        public string selectListId { get; set; }
        
    }
}