namespace PavlovRconWebserver.Models
{
    public class MatchTeamSelectedSteamIdentity : MatchSelectedSteamIdentity
    {
        public int TeamId { get; set; }
        public string OverWriteRole { get; set; }
    }
}