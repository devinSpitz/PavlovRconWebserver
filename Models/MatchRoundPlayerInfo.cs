using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchRoundPlayerInfo : PlayerModel
    {
        [BsonRef("PlayerModelExtended")]
        public PlayerModelExtended PlayerModelExtended { get; set; }
    }
}