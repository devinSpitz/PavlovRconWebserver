﻿using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    // This is not meant to go in to the database.
    // Its reader bean read or writen as its needed
    // Right now i don't think this this will happen that often.
    // May whithin a tournament it would be usfull but i don't think so

    public class PavlovServerWhitelistViewModel
    {
        
        public int pavlovServerId { get; set; } = 0;
        public bool Shack { get; set; }
        public List<string> steamIds { get; set; } = new();
    }
}