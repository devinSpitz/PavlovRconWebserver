using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class Team
    {
        public int Id { get; set; }
        public List<TeamSelectedSteamIdentity> TeamSelectedSteamIdentities { get; set; }

        [NotMapped] [BsonIgnore] public List<SteamIdentity> AllSteamIdentities { get; set; }

        public string Name { get; set; }
    }
}