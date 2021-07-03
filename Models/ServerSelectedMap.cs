using LiteDB;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class ServerSelectedMap
    {
        public int Id { get; set; }
        
        public string GameMode { get; set; }
        [BsonRef("Map")] 
        public virtual Map Map { get; set; }
        
        [BsonRef("PavlovServer")] 
        public virtual PavlovServer PavlovServer { get; set; }
    }
}