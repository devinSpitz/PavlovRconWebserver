using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class TeamSelectedSteamIdentity
    {
        public int Id { get; set; }

        [BsonRef("SteamIdentity")] public SteamIdentity SteamIdentity { get; set; }

        public string RoleOverwrite { get; set; }

        [BsonRef("Team")] public Team Team { get; set; }
    }
}