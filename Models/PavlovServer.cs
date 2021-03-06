using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public enum ServerType
    {
        Community = 1,
        Event = 2
    }
    public class PavlovServer
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [DisplayName("Rcon port")]
        public int TelnetPort { get; set; } = 9100;
        
        
        [Required]
        [DisplayName("Delete unused maps after x days:")]
        public int DeletAfter { get; set; } = 7;

        [DisplayName("Rcon password")]
        [Display(Description = "Will get converted if not md5 already")]
        public string TelnetPassword { get; set; }
        
        [Required]
        public int ServerPort { get; set; }

        [Required]
        public string ServerFolderPath { get; set; }
        
        
        [Required]
        public ServerType ServerType { get; set; }
        
        [BsonRef("SshServer")]
        public SshServer SshServer { get; set; }

    }
}