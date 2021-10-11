using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hangfire.Annotations;
using LiteDB;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace PavlovRconWebserver.Models
{
    public class SshServer
    {
        public int Id { get; set; }

        [Required] [DisplayName("IP")] public string Adress { get; set; }

        [Required] public string Name { get; set; }

        [DisplayName("SSH port")] public int SshPort { get; set; } = 22;

        [DisplayName("SSH username")] public string SshUsername { get; set; }
        [NotMapped] [BsonIgnore] public string NotRootSshUsername { get; set; }

        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassword { get; set; }

        [DisplayName("SSH key filename")]
        [Display(Description = "Select a filename")]
        public byte[] SshKeyFileName { get; set; }


        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphrase { get; set; }

        [NotMapped] [BsonIgnore] public List<PavlovServer> PavlovServers { get; set; }


        [DisplayName("Active Steam")]
        [Required]
        public bool SteamIsAvailable { get; set; } = false;

        [DisplayName("SteamCMD folder path")] public string SteamPath { get; set; }


        [DisplayName("Owner (OnPremise)")]
        [CanBeNull]
        public LiteDbUser Owner { get; set; }
        
        [BsonIgnore]
        [NotMapped]
        [CanBeNull]
        [DisplayName("SSH key file")]
        [Display(Description = "Select a file (only if you want to overwrite the possible existing one)")]
        public IFormFile SshKeyFileNameForm { get; set; }
        
        
        [NotMapped] [BsonIgnore] public List<LiteDbUser> LiteDbUsers { get; set; }
        [NotMapped] [BsonIgnore] public string LiteDbUserId { get; set; }
    }
}