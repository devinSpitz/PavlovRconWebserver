using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class TeamSelectedSteamIdentity
    {
        public int Id { get; set; }
        public long SteamIdentityId { get; set; }
    
        [BsonRef("SteamIdentity")] 
        public virtual SteamIdentity SteamIdentity { get; set; }
        
        public string RoleOverwrite { get; set; }
        public int TeamId { get; set; }
        [BsonRef("Team")] 
        public virtual Team Team { get; set; }
    }
}