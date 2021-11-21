using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace PavlovRconWebserver.Models
{
    public enum Status
    {
        Preparing = 1,
        StartetWaitingForPlayer = 2,
        OnGoing = 3,
        Finshed = 4
    }

    public class Match
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [DisplayName("Map")] public string MapId { get; set; }

        public string GameMode { get; set; }
        public bool ForceStart { get; set; } = false;
        public bool ForceSop { get; set; } = false;
        public int TimeLimit { get; set; }

        public int PlayerSlots { get; set; }
        [DisplayName("Score to end the game for teams or individually")] public int ScoreToEnd { get; set; } = 10;

        public Team Team0 { get; set; }
        public Team Team1 { get; set; }


        [BsonIgnore]
        [NotMapped]
        public List<MatchSelectedSteamIdentity> MatchSelectedSteamIdentities { get; set; } =
            new();

        [BsonIgnore]
        [NotMapped]
        public List<MatchTeamSelectedSteamIdentity> MatchTeam0SelectedSteamIdentities { get; set; } =
            new();

        [BsonIgnore]
        [NotMapped]
        public List<MatchTeamSelectedSteamIdentity> MatchTeam1SelectedSteamIdentities { get; set; } =
            new();

        public List<PavlovServerPlayer> PlayerResults { get; set; } = new();

        public ServerInfo EndInfo { get; set; } = new();

        [BsonRef("PavlovServer")] public PavlovServer PavlovServer { get; set; }

        public Status Status { get; set; } = Status.Preparing;


        public bool isEditable()
        {
            return Status == Status.Preparing;
        }

        public bool isFinished()
        {
            return Status == Status.Finshed;
        }

        public bool hasStats()
        {
            return Status == Status.OnGoing || Status == Status.Finshed;
        }

        public bool isStartable()
        {
            return Status == Status.Preparing;
        }

        public bool isForceStartable()
        {
            return Status == Status.StartetWaitingForPlayer;
        }

        public bool isForceStopatable()
        {
            return Status == Status.StartetWaitingForPlayer || Status == Status.OnGoing;
        }
    }
}