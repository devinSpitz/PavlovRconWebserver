using System.ComponentModel.DataAnnotations;

namespace PavlovRconWebserver.Models
{
    public class UpdateOverwriteRoleOfTeamSelectedSteamIdentityViewModel
    {
        [Required]
        public int teamId { get; set; }
        
        [Required]
        public long steamIdentityId { get; set; }
        
        [Required]
        public string overWriteRole { get; set; }
    }
}