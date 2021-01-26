using System;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class ServerBans
    {
        public int Id { get; set; }
        
        public string SteamId { get; set; }
        
        public string SteamName { get; set; } // Will always get overwritten with whatever comes from the server
        
        public DateTime BannedDateTime { get; set; }
        
        public TimeSpan BanSpan { get; set; }
        public string Comment { get; set; }
        
        [BsonRef("PavlovServer")] 
        public virtual PavlovServer PavlovServer { get; set; }
    }
}