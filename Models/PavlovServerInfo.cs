using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerInfo : ServerInfo
    {
        [Key]
        public int ServerId { get; set; }
    }
}