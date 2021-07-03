using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public class PavlovServerGameIniMap
    {
        public string MapLabel = "";
        public string GameMode = "";
    }
}