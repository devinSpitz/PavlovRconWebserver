using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class SelectedServerMapsViewModel
    {
        public List<ServerSelectedMap> SelectedMaps { get; set; }
        public List<Map> AllMaps { get; set; }
        public int ServerId { get; set; }
    }
}