using System.Collections.Generic;
using LiteDB.Identity.Models;

namespace PavlovRconWebserver.Models
{
    public class RoleEdit
    {
        public LiteDbRole Role { get; set; }
        public IEnumerable<LiteDbUser> Members { get; set; }
        public IEnumerable<LiteDbUser> NonMembers { get; set; }
    }
}