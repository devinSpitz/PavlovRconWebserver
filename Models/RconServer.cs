using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        
        [DisplayName("Use telnet directly")]
        [Display(Description = "Not recommended")]
        public bool UseTelnet { get; set; }
        
        [DisplayName("Rcon password")]
        [Display(Description = "Will get converted if not md5 already")]
        public string Password { get; set; }

        
        [DisplayName("Use SSH")]
        public bool UseSsh { get; set; }
        
        [DisplayName("SSH username")]
        public string SshUsername { get; set; } 
        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassword { get; set; }
        
        [DisplayName("SSH key filename")]//Todo maybe chosable from javascript that scans the folder or even upload but that check security
        [Display(Description = "Put in the name of the file in the keyfile folder")]
        public string SshKeyFileName { get; set; }
        
        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphrase { get; set; }
        
        
    }
}