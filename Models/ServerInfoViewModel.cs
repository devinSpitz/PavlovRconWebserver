namespace PavlovRconWebserver.Models
{
    public class ServerInfoViewModel
    {
        public ServerInfo ServerInfo { get; set; }
        public string Name { get; set; }
    }

    public class ServerInfo
    {
        
        public string MapLabel { get; set; }
        public string GameMode { get; set; }
        public string ServerName { get; set; }
        public string RoundState { get; set; }
        public string PlayerCount { get; set; }
    }
}