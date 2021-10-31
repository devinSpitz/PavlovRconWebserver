using System;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityStatsServerViewModel : SteamIdentityStatsServer
    {
        public string serverName { get; set; } = "";
    }
}