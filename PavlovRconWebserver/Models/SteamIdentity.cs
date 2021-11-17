using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;
using LiteDB.Identity.Models;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentity
    {
        [Required] [DisplayName("SteamID64")] public string Id { get; set; }
        [Required] [DisplayName("Oculus name")] public string OculusId { get; set; }

        [Required] [DisplayName("SteamName")] public string Name { get; set; }


        [DisplayName("Costume")] public string Costume { get; set; }

        [BsonRef("LiteDbUser")] public virtual LiteDbUser LiteDbUser { get; set; }


        [NotMapped] [BsonIgnore] public string LiteDbUserId { get; set; }

        [NotMapped] [BsonIgnore] public List<LiteDbUser> LiteDbUsers { get; set; }

        [NotMapped] 
        [BsonIgnore]
        public string ShowName => Id + ", " + Name;
        [NotMapped] 
        [BsonIgnore]
        public string ShowOculusName => Id + ", " + Name;
    }
}