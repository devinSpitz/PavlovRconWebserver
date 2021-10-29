using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityStatsServer
    {
        public ObjectId Id { get; set; }
        public string SteamId { get; set; } = "";
        public string SteamName { get; set; } = "";
        public string SteamPicture { get; set; } = "";
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Assists { get; set; } = 0;
        public int Level { get; set; } = 0;
        public int LastAddedScore { get; set; } = 0;
        public int Exp { get; set; } = 0;
        public int serverId { get; set; } = 0;
    }
}