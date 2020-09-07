using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class RconServer
    {
        public int Id { get; set; }
        [Required]
        
        [DisplayName("IP")]
        public string Adress { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        [DisplayName("Rcon port")]
        public int TelnetPort { get; set; } = 9100;

        [DisplayName("SSH port")]
        public int SshPort { get; set; } = 22;
        
        
        [DisplayName("Rcon password")]
        [Display(Description = "Will get converted if not md5 already")]
        public string Password { get; set; }

        [DisplayName("SSH username")]
        public string SshUsername { get; set; } 
        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassword { get; set; }
        
        [DisplayName("SSH key filename")]
        [Display(Description = "Select a filename")]
        public string SshKeyFileName { get; set; }
        [NotMapped]
        public List<string> SshKeyFileNames { get; set; } = new List<string>();
        
        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphrase { get; set; }
        
        [DisplayName("Pavlov server path on server")]
        public string PavlovServerPath { get; set; }


        
    }
}