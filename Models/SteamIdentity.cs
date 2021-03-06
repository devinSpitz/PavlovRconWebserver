using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;
using LiteDB.Identity.Models;
using PavlovRconWebserver.Extensions;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentity
    {
        [Required]
        
        [DisplayName("SteamID64")]
        public long Id { get; set; }
        [Required]
        
        [DisplayName("SteamName")]
        public string Name { get; set; }

        [BsonRef("LiteDbUser")]
        public virtual LiteDbUser LiteDbUser { get; set; }
        
        
        [NotMapped][BsonIgnore]
        public string LiteDbUserId { get; set; }
        
        [NotMapped][BsonIgnore]
        public List<LiteDbUser> LiteDbUsers { get; set; }
    }
}