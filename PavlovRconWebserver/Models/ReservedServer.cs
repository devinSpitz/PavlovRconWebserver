using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class ReservedServer
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public int? ServerId { get; set; }
        public int? SshServerId { get; set; }
    }
}