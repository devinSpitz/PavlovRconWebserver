using System.Collections.Generic;
using Hangfire.Annotations;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerListPublicViewModel
    {
        public int MatchId { get; set; }
        public ServerInfo ServerInfo { get; set; }
        public List<PlayerModelExtended> PlayerList { get; set; }
        public string team0Score { get; set; }
        public string team1Score { get; set; }
        [CanBeNull]
        public int serverId { get; set; }
        [CanBeNull]
        public bool withMaps { get; set; }
    }
    
    public class PavlovServerPlayerListPublicViewModelEntry : PavlovServerPlayerListPublicViewModel
    {
        
        public string UniqueId { get; set; }
        public int TeamId { get; set; } = 0;
        public bool Team { get; set; } = false;

    }
}