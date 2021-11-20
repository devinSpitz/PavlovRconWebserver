using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchListViewModel
    {
        public List<SteamIdentity> SteamIdentities { get; set; }

        public string selectListId { get; set; }
        public string IdUsed { get; set; } = "Id";

    }
}