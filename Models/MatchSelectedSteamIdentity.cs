using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchSelectedSteamIdentity
    {
        public int Id { get; set; }
        public long SteamIdentityId { get; set; }
    
        [BsonRef("SteamIdentity")] 
        public virtual SteamIdentity SteamIdentity { get; set; }
        public int MatchId { get; set; }
        [BsonRef("Match")] 
        public virtual Match Match { get; set; }
    }
}