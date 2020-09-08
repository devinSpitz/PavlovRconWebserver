using System.Collections.Generic;

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
        public virtual ICollection<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; }
        public virtual ICollection<MatchRound> MatchRound { get; set; }
        public GameMode GameMode { get; set; }
        public int RconServerId { get; set; }
        public virtual RconServer RconServer { get; set; }
        public Status Status { get; set; } = Status.Preparing;
    }
}