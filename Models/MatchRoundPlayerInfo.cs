using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchRoundPlayerInfo : PlayerModel
    {
        public int Id { get; set; }
        [BsonRef("MatchRound")]
        public MatchRound MatchRound { get; set; }
    }
}