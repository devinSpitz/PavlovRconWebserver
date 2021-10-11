using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    //Todo: IConvertible
    public class PavlovServerViewModel : PavlovServer
    {
        public int sshServerId { get; set; }
        public bool create { get; set; } = false;
        public bool remove { get; set; } = false;
        [DisplayName("SSH username")] public string SshUsernameRoot { get; set; }

        [DisplayName("SSH password")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPasswordRoot { get; set; }

        [DisplayName("SSH key filename")]
        [Display(Description = "Select a filename")]
        public byte[] SshKeyFileNameRoot { get; set; }

        [DisplayName("SSH passphrase")]
        [Display(Description = "CAUTION: WILL BE SAVED BLANK")]
        public string SshPassphraseRoot { get; set; }

        [NotMapped] [BsonIgnore] public List<string> SshKeyFileNames { get; set; } = new();

        public PavlovServerViewModel fromPavlovServer(PavlovServer pavlovServer, int sshServerId)
        {
            return new()
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
                sshServerId = sshServerId,
                Owner = pavlovServer.Owner
            };
        }

        public PavlovServer toPavlovServer(PavlovServerViewModel viewModel)
        {
            return new()
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
                DeletAfter = viewModel.DeletAfter,
                Owner = viewModel.Owner
            };
        }
    }
}