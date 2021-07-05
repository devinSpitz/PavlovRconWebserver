using LiteDB;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class ServerSelectedMods
    {
        public int Id { get; set; }

        [BsonRef("LiteDbUser")]
        public virtual LiteDbUser LiteDbUser { get; set; }
        
        [BsonRef("PavlovServer")] 
        public virtual PavlovServer PavlovServer { get; set; }
    }
}