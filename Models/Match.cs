using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public enum GameMode
    {
        SND = 1,
        TDM = 2,
        DmBrHg= 3
    }
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
        
        public int Rounds { get; set; }
        
        public Team TeamA { get; set; }
        public Team TeamB { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<Team> AllTeams { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<MatchRound> MatchRound { get; set; }
        
        public GameMode GameMode { get; set; }
        
        [BsonRef("RconServer")]
        public PavlovServer PavlovServer { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<PavlovServer> AllPavlovServers { get; set; }
        public Status Status { get; set; } = Status.Preparing;
    }
}