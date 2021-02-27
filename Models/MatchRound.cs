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

        public int Team0Score { get; set; } = 0;
        public int Team01core { get; set; } = 0;
        [BsonIgnore][NotMapped]
        public List<PlayerModelExtended> MatchRoundPlayerInfo { get; set; }
    }
}