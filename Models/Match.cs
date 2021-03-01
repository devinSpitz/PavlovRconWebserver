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
        public List<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; } =
            new List<MatchSelectedSteamIdentity>();

        [BsonIgnore][NotMapped]
        public List<MatchTeamSelectedSteamIdentity> MatchTeam0SelectedSteamIdentities { get; set; } =
            new List<MatchTeamSelectedSteamIdentity>();

        [BsonIgnore][NotMapped]
        public List<MatchTeamSelectedSteamIdentity> MatchTeam1SelectedSteamIdentities { get; set; } =
            new List<MatchTeamSelectedSteamIdentity>();

        public List<MatchRound> MatchRound { get; set; }
        
        [BsonRef("PavlovServer")]
        public PavlovServer PavlovServer { get; set; }
        public Status Status { get; set; } = Status.Preparing;
        

        
    }
}