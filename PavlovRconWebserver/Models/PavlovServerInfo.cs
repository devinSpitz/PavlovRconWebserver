namespace PavlovRconWebserver.Models
{
    public class PavlovServerInfo : ServerInfo
    {
        public int ServerId { get; set; }
        public int Round { get; set; } = 0;
    }
}