using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class MatchSelectedSteamIdentity
    {
        public int Id { get; set; }


        public string SteamIdentityId { get; set; }

        [BsonIgnore] [NotMapped] public virtual SteamIdentity SteamIdentity { get; set; }

        public int matchId { get; set; }

        [BsonIgnore] [NotMapped] public virtual Match Match { get; set; }
    }
}