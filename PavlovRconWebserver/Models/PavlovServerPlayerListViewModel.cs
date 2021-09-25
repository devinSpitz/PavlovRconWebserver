using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerListViewModel
    {
        public List<PlayerModelExtended> PlayerList { get; set; }
        public string team0Score { get; set; }
        public string team1Score { get; set; }
    }
}