using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class ConnectionResult
    {
        public bool Success { get; set; } = false;
        public List<string> errors { get; set; } = new List<string>();
        public string answer { get; set; } = "";
        
        public List<string> MultiAnswer { get; set; } = new List<string>();
    }
}