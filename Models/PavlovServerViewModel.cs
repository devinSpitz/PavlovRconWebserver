using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerViewModel: PavlovServer
    {
        public int sshServerId { get; set; }

        public PavlovServerViewModel fromPavlovServer(PavlovServer pavlovServer,int sshServerId)
        {
            return new PavlovServerViewModel()
            {
                Id = pavlovServer.Id,
                Name = pavlovServer.Name,
                ServerFolderPath = pavlovServer.ServerFolderPath,
                ServerPort = pavlovServer.ServerPort,
                ServerType = pavlovServer.ServerType,
                TelnetPassword = pavlovServer.TelnetPassword,
                TelnetPort = pavlovServer.TelnetPort,
                sshServerId = sshServerId
            };
        }
        
        public PavlovServer toPavlovServer(PavlovServerViewModel viewModel)
        {
            return new PavlovServer()
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                ServerFolderPath = viewModel.ServerFolderPath,
                SshServer = viewModel.SshServer,
                ServerType = viewModel.ServerType,
                ServerPort = viewModel.ServerPort,
                TelnetPassword = viewModel.TelnetPassword,
                TelnetPort = viewModel.TelnetPort,
            };


        }
        
    }
}