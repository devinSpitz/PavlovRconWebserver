using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerHistory : PavlovServerPlayer
    {
        public DateTime date { get; set; }
    }
}