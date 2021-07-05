using LiteDB;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class ServerSelectedWhiteList
    {
        public int Id { get; set; }
        public string SteamIdentityId { get; set; }
        
        [BsonRef("PavlovServer")] 
        public virtual PavlovServer PavlovServer { get; set; }
    }
}