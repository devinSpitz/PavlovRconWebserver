using System.Collections.Generic;
using System.ComponentModel;

namespace PavlovRconWebserver.Models
{
    public class RconServer
    {
        public int Id { get; set; }
        public string Adress { get; set; }
        public int Port { get; set; }
        [DisplayName("Password (md5)")]
        public string Password { get; set; }

    }
}