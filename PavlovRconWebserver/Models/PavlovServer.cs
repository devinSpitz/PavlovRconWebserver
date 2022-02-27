using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hangfire.Annotations;
using LiteDB;
using LiteDB.Identity.Models;

namespace PavlovRconWebserver.Models
{
    public enum ServerType
    {
        Community = 1,
        Event = 2
    }

    //There is no state enabled cause when its enabled its either active or inactive
    //Event server should be disabled otherwise they do autostart xD
    public enum ServerServiceState
    {
        none = 1,
        active = 2,
        inactive = 3,
        disabled = 4
    }

    public class PavlovServer
    {
        public int Id { get; set; }

        [Required] [DisplayName("Gui Name")] public string Name { get; set; }

        [Required] [DisplayName("Rcon port")] public int TelnetPort { get; set; } = 9100;

        [Required]
        [DisplayName("Delete unused maps after x days:")]
        public int DeletAfter { get; set; } = 7;

        [DisplayName("Rcon password")]
        [Display(Description = "Will get converted if not md5 already")]
        public string TelnetPassword { get; set; }

        [Required] public int ServerPort { get; set; } = 7777;

        [Required] public string ServerFolderPath { get; set; }

        [Required]
        [DisplayName("Service name (no .service and special chars)")]
        public string ServerSystemdServiceName { get; set; }


        [Required] public ServerType ServerType { get; set; }

        [DisplayName("Server service state")]
        public ServerServiceState ServerServiceState { get; set; } = ServerServiceState.none;

        [BsonRef("SshServer")] public SshServer SshServer { get; set; }

        [DisplayName("Owner (ServerRent)")]
        [CanBeNull]
        public LiteDbUser Owner { get; set; }
        
        [CanBeNull]
        public LiteDbUser OldOwner { get; set; }


        [DisplayName("Autobalance(WIP)*")]
        [CanBeNull]
        public bool AutoBalance { get; set; } = false;
        [CanBeNull]
        public DateTime? AutoBalanceLast { get; set; } = null;
        [DisplayName("AB cooldown (min)")]
        public int AutoBalanceCooldown { get; set; } = 15;
        [DisplayName("Save Stats**")]
        [CanBeNull]
        public bool SaveStats { get; set; } = false;
        [DisplayName("Shack")]
        [CanBeNull]
        public bool Shack { get; set; } = false;
        [DisplayName("Use Global Bans")]
        public bool GlobalBan { get; set; } = false;
        
        
        
        
        
        [NotMapped] [BsonIgnore] public List<LiteDbUser> LiteDbUsers { get; set; }
        [NotMapped] [BsonIgnore] public string LiteDbUserId { get; set; }
    }
}