using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class Match
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rounds { get; set; }
        public Team TeamA { get; set; }
        public Team TeamB { get; set; }
        public virtual ICollection<MatchRound> MatchRound { get; set; }
    }
}