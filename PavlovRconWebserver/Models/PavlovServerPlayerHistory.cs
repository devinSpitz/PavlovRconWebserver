using System;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerPlayerHistory : PavlovServerPlayer
    {
        public ObjectId Id { get; set; }
        public DateTime date { get; set; }
    }
}