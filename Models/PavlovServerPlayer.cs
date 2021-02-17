using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayer : PlayerModelExtended
    {
        public int ServerId { get; set; }
    }
}