using System.Collections.Generic;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPublicMapListViewModel
    {
        public int MatchId { get; set; }
        public ServerInfo ServerInfo { get; set; }
        public ServerSelectedMap[] MapList { get; set; }
    }
}