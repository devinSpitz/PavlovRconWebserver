using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerInfo : ServerInfo
    {
        public int ServerId { get; set; }
    }
}