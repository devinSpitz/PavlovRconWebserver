using System.Collections.Generic;
using System.ComponentModel;

namespace PavlovRconWebserver.Models
{
    public class SteamIdentityMatchTeamViewModel
    {
        public List<Team> AvailableTeams { get; set; }  = new List<Team>();
        public int? selectedTeam0 { get; set; }
        public int? selectedTeam1 { get; set; }
        public List<SteamIdentity> SelectedSteamIdentitiesTeam0 { get; set; } = new List<SteamIdentity>();
        public List<SteamIdentity> SelectedSteamIdentitiesTeam1 { get; set; } = new List<SteamIdentity>();
    }
    
    public class SteamIdentityMatchTeamSingleViewModel
    {
        [DisplayName("Available Teams")]
        public List<Team> AvailableTeams { get; set; }
        
        
        public int? selectedTeam { get; set; }
        [DisplayName("Selected Steam Identities")]
        public List<SteamIdentity> SelectedSteamIdentities { get; set; } = new List<SteamIdentity>();
        [DisplayName("Available Steam Identities")]
        public List<SteamIdentity> AvailableSteamIdentities { get; set; } = new List<SteamIdentity>();
        public string SelectId  { get; set; } = "";
        public string SelectedId  { get; set; } = "";
        public string TeamsDropDownId  { get; set; } = "";
        public string SelectButtonId  { get; set; } = "";
    }
}