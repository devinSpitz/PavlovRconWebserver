using LiteDB;
using LiteDB.Identity.Models;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentity
    {
        public long Id { get; set; }
        public ObjectId LiteDbUserId { get; set; }
        public virtual LiteDbUser LiteDbUser { get; set; }
        public string RoleOverwrite { get; set; }
    }
}