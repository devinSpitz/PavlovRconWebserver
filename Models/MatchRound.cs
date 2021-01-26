using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchRound
    {
        public int Id { get; set; }
        [BsonRef("Match")]
        public Match Match { get; set; }
        
        [BsonIgnore][NotMapped]
        public List<MatchRoundPlayerInfo> MatchRoundPlayerInfo { get; set; }
    }
}