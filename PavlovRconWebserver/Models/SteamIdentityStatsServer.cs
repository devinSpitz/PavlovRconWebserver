using System;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityStatsServer
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string SteamId { get; set; } = "";
        public string SteamName { get; set; } = "";
        public string SteamPicture { get; set; } = "";
        public int Kills { get; set; } = 0;
        public int LastAddedKills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int LastAddedDeaths { get; set; } = 0;
        public int Assists { get; set; } = 0;
        public int LastAddedAssists { get; set; } = 0;
        public int LastAddedScore { get; set; } = 0;
        public int Exp { get; set; } = 0;
        public int ServerId { get; set; } = 0;
        public int ForRound { get; set; } = 0;
        public TimeSpan UpTime { get; set; } = new TimeSpan(0, 0, 0, 0);
        public DateTime logDateTime { get; set; } = DateTime.Now;
    }
}