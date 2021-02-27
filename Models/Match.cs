using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public enum Status
    {
        Preparing = 1,
        OnGoing = 2,
        Finshed= 3
    }
    public class Match
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        [DisplayName("Map")]
        public string MapId { get; set; }
        
        public string GameMode { get; set; }
        
        public Team Team0 { get; set; }
        public Team Team1 { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<Team> AllTeams { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<MatchRound> MatchRound { get; set; }
        
        // public GameMode GameMode { get; set; }
        
        [BsonRef("PavlovServer")]
        public PavlovServer PavlovServer { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<PavlovServer> AllPavlovServers { get; set; }
        public Status Status { get; set; } = Status.Preparing;
    }
}