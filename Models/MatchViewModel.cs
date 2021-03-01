using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchViewModel : Match
    {
        [BsonIgnore] [NotMapped] 
        public List<Team> AllTeams { get; set; }

        [BsonIgnore] [NotMapped] 
        public int? Team0Id { get; set; }
        [BsonIgnore] [NotMapped] 
        public int? Team1Id { get; set; }
        [BsonIgnore][NotMapped]
        public List<PavlovServer> AllPavlovServers { get; set; }
        [BsonIgnore][NotMapped]
        public int PavlovServerId { get; set; }
        
        [BsonIgnore]
        [NotMapped]
        public List<string> MatchSelectedSteamIdentitiesStrings { get; set; } = new List<string>();
        [BsonIgnore]
        [NotMapped]
        public List<string> MatchTeam0SelectedSteamIdentitiesStrings { get; set; } = new List<string>();
        [BsonIgnore]
        [NotMapped]
        public List<string> MatchTeam1SelectedSteamIdentitiesStrings { get; set; } = new List<string>();

    }
}