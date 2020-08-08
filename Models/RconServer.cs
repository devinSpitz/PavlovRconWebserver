using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Renci.SshNet;

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
        [DisplayName("Telnet port")]
        public int TelnetPort { get; set; } = 9100;

        [DisplayName("SSH port")]
        public int SshPort { get; set; } = 22;
        
        [DisplayName("Use telnet directly (not recommended!)")]
        public bool UseTelnet { get; set; }
        
        [Required]
        [DisplayName("Rcon password (md5)")]
        public string Password { get; set; }

        
        [DisplayName("Use SSH")]
        public bool UseSsh { get; set; }
        
        [DisplayName("SSH username")]
        public string SshUsername { get; set; } 
        [DisplayName("SSH password (CAUTION: WILL BE SAVED BLANK)")]
        public string SshPassword { get; set; }
        
        [DisplayName("SSH key filename (put in the name of the file in the keyfile folder)")]//Todo maybe chosable from javascript that scans the folder or even upload but that check security
        public string SshKeyFileName { get; set; }
        
        [DisplayName("SSH passphrase (CAUTION: WILL BE SAVED BLANK)")]
        public string SshPassphrase { get; set; }
        
        
    }
}