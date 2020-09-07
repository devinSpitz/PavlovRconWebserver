using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class MatchRound
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public virtual Match Match { get; set; }
        public virtual ICollection<MatchRoundPlayerInfo> MatchRoundPlayerInfo { get; set; }
    }
}