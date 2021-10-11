using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerListPublicViewModel
    {
        public int MatchId { get; set; }
        public ServerInfo ServerInfo { get; set; }
        public List<PlayerModelExtended> PlayerList { get; set; }
        public string team0Score { get; set; }
        public string team1Score { get; set; }
    }
}