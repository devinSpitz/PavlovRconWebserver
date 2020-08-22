using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class RoleEdit
    {
        public AspNetCore.Identity.LiteDB.IdentityRole Role { get; set; }
        public IEnumerable<InbuildUser> Members { get; set; }
        public IEnumerable<InbuildUser> NonMembers { get; set; }
    }
}
