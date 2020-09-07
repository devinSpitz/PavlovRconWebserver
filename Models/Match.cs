using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public enum GameMode
    {
        SND = 1,
        TDM = 2,
        DmBrHg= 3
    }
    public class Match
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rounds { get; set; }
        public Team TeamA { get; set; }
        public Team TeamB { get; set; }
        public ICollection<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; }
        public virtual ICollection<MatchRound> MatchRound { get; set; }
        public GameMode GameMode { get; set; }
    }
}