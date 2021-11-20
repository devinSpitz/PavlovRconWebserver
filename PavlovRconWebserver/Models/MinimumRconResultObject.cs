using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class MinimumRconResultObject
    {
        public bool Successful { get; set; } = false;
        public string Command { get; set; } = "";
        
    }
}