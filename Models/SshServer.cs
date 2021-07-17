using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class SshServer
    {
        public int Id { get; set; }

        [Required] [DisplayName("IP")] public string Adress { get; set; }

        [Required] public string Name { get; set; }

        [DisplayName("SSH port")] public int SshPort { get; set; } = 22;

        [DisplayName("SSH username")] public string SshUsername { get; set; }

        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassword { get; set; }

        [DisplayName("SSH key filename")]
        [Display(Description = "Select a filename")]
        public string SshKeyFileName { get; set; }

        [NotMapped] [BsonIgnore] public List<string> SshKeyFileNames { get; set; } = new List<string>();

        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphrase { get; set; }

        [NotMapped] [BsonIgnore] public List<PavlovServer> PavlovServers { get; set; }



        [DisplayName("Active Steam")]
        [Required] public bool SteamIsAvailable { get; set; } = false; 
        public string SteamUser { get; set; }
        [DisplayName("SteamCMD folder path")]
        public string SteamPath { get; set; }
    }
}