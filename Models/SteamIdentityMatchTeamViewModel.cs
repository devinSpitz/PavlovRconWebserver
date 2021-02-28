using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchTeamViewModel
    {
        public List<Team> AvailableTeams { get; set; }  = new List<Team>();
        public List<SteamIdentity> SelectedSteamIdentitiesTeam0 { get; set; } = new List<SteamIdentity>();
        public List<SteamIdentity> SelectedSteamIdentitiesTeam1 { get; set; } = new List<SteamIdentity>();
    }
    
    public class SteamIdentityMatchTeamSingleViewModel
    {
        
        public List<Team> AvailableTeams { get; set; }
        public List<SteamIdentity> SelectedSteamIdentities { get; set; } = new List<SteamIdentity>();
        public List<SteamIdentity> AvailableSteamIdentities { get; set; } = new List<SteamIdentity>();
        public string SelectId  { get; set; } = "";
        public string SelectedId  { get; set; } = "";
        public string TeamsDropDownId  { get; set; } = "";
        public string SelectButtonId  { get; set; } = "";
    }
}