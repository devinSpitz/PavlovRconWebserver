using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerViewModel: PavlovServer
    {
        public int rconServerId { get; set; }

        public PavlovServerViewModel fromPavlovServer(PavlovServer pavlovServer,int rconServerId)
        {
            return new PavlovServerViewModel()
            {
                Id = pavlovServer.Id,
                Name = pavlovServer.Name,
                ServerFolderPath = pavlovServer.ServerFolderPath,
                ServerPort = pavlovServer.ServerPort,
                TelnetPassword = pavlovServer.TelnetPassword,
                TelnetPort = pavlovServer.TelnetPort,
                rconServerId = rconServerId
            };
        }
        
        public PavlovServer toPavlovServer(PavlovServerViewModel viewModel)
        {
            return new PavlovServer()
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                ServerFolderPath = viewModel.ServerFolderPath,
                RconServer = viewModel.RconServer,
                ServerPort = viewModel.ServerPort,
                TelnetPassword = viewModel.TelnetPassword,
                TelnetPort = viewModel.TelnetPort,
            };


        }
        
    }
}