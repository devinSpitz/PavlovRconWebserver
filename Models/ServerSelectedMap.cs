using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class ServerSelectedMap
    {
        public int Id { get; set; }
        
        [BsonRef("Map")] 
        public virtual Map Map { get; set; }
        
        [BsonRef("RconServer")] 
        public virtual RconServer RconServer { get; set; }
    }
}