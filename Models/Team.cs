using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class Team
    {
        public int Id { get; set; }
        public ICollection<TeamSelectedSteamIdentity> TeamSelectedSteamIdentities { get; set; }
        
        [NotMapped]
        public ICollection<SteamIdentity> AllSteamIdentities { get; set; }
        public string Name { get; set; }
    }
}