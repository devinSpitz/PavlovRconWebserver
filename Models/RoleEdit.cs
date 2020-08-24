using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class RoleEdit
    {
        public LiteDB.Identity.Models.LiteDbRole Role { get; set; }
        public IEnumerable<LiteDB.Identity.Models.LiteDbUser> Members { get; set; }
        public IEnumerable<LiteDB.Identity.Models.LiteDbUser> NonMembers { get; set; }
    }
}
