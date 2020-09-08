using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PavlovRconWebserver.Models
{
    public class PavlovServer
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [DisplayName("Rcon port")]
        public int TelnetPort { get; set; } = 9100;

        [Required]
        [DisplayName("Rcon password")]
        [Display(Description = "Will get converted if not md5 already")]
        public string TelnetPassword { get; set; }
        
        [Required]
        public int ServerPort { get; set; }
        
        [Required]
        public string ServerFolderPath { get; set; }
        
        public int RconServerId { get; set; }
        public RconServer RconServer { get; set; }
    }
}