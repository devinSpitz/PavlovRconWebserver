using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerListPublicViewModel
    {
        public ServerInfo ServerInfo { get; set; }
        public List<PlayerModelExtended> PlayerList { get; set; }
        public string team0Score { get; set; }
        public string team1Score { get; set; }
    }
}