using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerViewModel : PavlovServer
    {
        public int sshServerId { get; set; }
        public bool create { get; set; } = false;        
        [DisplayName("SSH username")] public string SshUsernameRoot { get; set; }

        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPasswordRoot { get; set; }

        [DisplayName("SSH key filename")]
        [Display(Description = "Select a filename")]
        public string SshKeyFileNameRoot { get; set; }
        
        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphraseRoot { get; set; }

        [NotMapped] [BsonIgnore] public List<string> SshKeyFileNames { get; set; } = new List<string>();

        public PavlovServerViewModel fromPavlovServer(PavlovServer pavlovServer, int sshServerId)
        {
            return new PavlovServerViewModel
            {
                Id = pavlovServer.Id,
                Name = pavlovServer.Name,
                ServerFolderPath = pavlovServer.ServerFolderPath,
                ServerSystemdServiceName = pavlovServer.ServerSystemdServiceName,
                ServerPort = pavlovServer.ServerPort,
                ServerType = pavlovServer.ServerType,
                TelnetPassword = pavlovServer.TelnetPassword,
                TelnetPort = pavlovServer.TelnetPort,
                DeletAfter = pavlovServer.DeletAfter,
                sshServerId = sshServerId
            };
        }

        public PavlovServer toPavlovServer(PavlovServerViewModel viewModel)
        {
            return new PavlovServer
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                ServerFolderPath = viewModel.ServerFolderPath,
                ServerSystemdServiceName = viewModel.ServerSystemdServiceName,
                SshServer = viewModel.SshServer,
                ServerType = viewModel.ServerType,
                ServerPort = viewModel.ServerPort,
                TelnetPassword = viewModel.TelnetPassword,
                TelnetPort = viewModel.TelnetPort,
                DeletAfter = viewModel.DeletAfter
            };
        }
    }
}