using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using PavlovRconWebserver.Extensions;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Models
{
    // This is not meant to go in to the database.
    // Its reader bean read or writen as its needed
    // Right now i don't think this this will happen that often.
    // May whithin a tournament it would be usfull but i don't think so
    
    public class PavlovServerModlistViewModel
    {
        public int pavlovServerId { get; set; } = 0;
        public List<string> userIds { get; set; } = new List<string>();

    }
}