namespace PavlovRconWebserver.Models
{
    public class MatchRoundPlayerInfo : PlayerModel
    {
        public int Id { get; set; }
        public int MatchRoundId { get; set; }
        public virtual MatchRound MatchRound { get; set; }
    }
}