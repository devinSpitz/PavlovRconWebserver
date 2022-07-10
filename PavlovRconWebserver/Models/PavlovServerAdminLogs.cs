using System;
using LiteDB.Identity.Models;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerAdminLogs
    {
        public int Id { get; set; }
        public LiteDbUser Executor { get; set; }
        public int ServerId { get; set; }
        public string CommandExecuted { get; set; } = "";
        public string CommandResult { get; set; } = "";
        public DateTime Time { get; set; } = DateTime.Now;
    }
}