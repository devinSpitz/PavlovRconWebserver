using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchTeamSelectedSteamIdentity : MatchSelectedSteamIdentity
    {
        public int TeamId { get; set; }
    }
}