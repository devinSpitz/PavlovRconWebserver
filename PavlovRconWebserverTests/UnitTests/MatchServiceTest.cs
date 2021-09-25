using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class MatchServiceTest
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly TeamService _teamService;
        private readonly MatchService _matchService;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly MatchSelectedSteamIdentitiesService _matchSelectedSteamIdentitiesService;
        private readonly MatchSelectedTeamSteamIdentitiesService _matchSelectedTeamSteamIdentitiesService;
        private readonly UserManager<LiteDbUser> _userManager;
        public MatchServiceTest() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _teamService = _mocker.CreateInstance<TeamService>();
            _matchService = _mocker.CreateInstance<MatchService>();
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
            _matchSelectedSteamIdentitiesService = _mocker.CreateInstance<MatchSelectedSteamIdentitiesService>();
            _matchSelectedTeamSteamIdentitiesService = _mocker.CreateInstance<MatchSelectedTeamSteamIdentitiesService>();
            _userManager = services.GetUserManager();

        }
        
        public static Match CreateMatch(MatchService matchService,PavlovServer pavlovServer,bool teamMatch,TeamService teamService,SteamIdentityService steamIdentityService, UserManager<LiteDbUser> userManager,MatchSelectedSteamIdentitiesService matchSelectedSteamIdentitiesService,MatchSelectedTeamSteamIdentitiesService matchSelectedTeamSteamIdentitiesService)
        {
            
            var user = ServerSelectedModServiceTests.InsertUserAndSteamIdentity(steamIdentityService, userManager,"test","1");
            var user2 = ServerSelectedModServiceTests.InsertUserAndSteamIdentity(steamIdentityService, userManager,"test2","2");
            var steamIdentity1 = steamIdentityService.FindOne(user.Id).GetAwaiter().GetResult();
            var steamIdentity2 = steamIdentityService.FindOne(user2.Id).GetAwaiter().GetResult();
            var team1 = TeamSelectedSteamIdentityServiceTest.CreateTeam(teamService, "test");
            var team2 = TeamSelectedSteamIdentityServiceTest.CreateTeam(teamService, "test2");
            if (teamMatch)
            {

                matchService.Upsert(new Match
                {
                    Name = "Test",
                    MapId = "null",
                    GameMode = "TDM",
                    ForceStart = false,
                    ForceSop = false,
                    TimeLimit = 40,
                    PlayerSlots = 10,
                    Team0 = team1,
                    Team1 = team2,
                    PavlovServer = pavlovServer,
                    Status = Status.Preparing
                }).GetAwaiter().GetResult();
            }
            else
            {
                
                matchService.Upsert(new Match
                {
                    Name = "Test",
                    MapId = "null",
                    GameMode = "GUN",
                    ForceStart = false,
                    ForceSop = false,
                    TimeLimit = 40,
                    PlayerSlots = 10,
                    Team0 = team1,
                    PavlovServer = pavlovServer,
                    Status = Status.Preparing
                }).GetAwaiter().GetResult();
            }


            var match = matchService.FindAll().GetAwaiter().GetResult().FirstOrDefault();
            if (teamMatch)
            {
                match.MatchTeam0SelectedSteamIdentities = new List<MatchTeamSelectedSteamIdentity>
                {
                    new MatchTeamSelectedSteamIdentity
                    {
                        SteamIdentityId = steamIdentity1.Id,
                        SteamIdentity = steamIdentity1,
                        matchId = match.Id,
                        TeamId = 0
                    }
                };
                match.MatchTeam1SelectedSteamIdentities = new List<MatchTeamSelectedSteamIdentity>()
                {
                    new MatchTeamSelectedSteamIdentity
                    {
                        SteamIdentityId = steamIdentity2.Id,
                        SteamIdentity = steamIdentity2,
                        matchId = match.Id,
                        TeamId = 1
                    }
                };
                matchSelectedTeamSteamIdentitiesService.Upsert(match.MatchTeam0SelectedSteamIdentities,match.Id,0).GetAwaiter().GetResult();
                matchSelectedTeamSteamIdentitiesService.Upsert(match.MatchTeam1SelectedSteamIdentities,match.Id,1).GetAwaiter().GetResult();
            }
            else
            {
                match.MatchSelectedSteamIdentities = new List<MatchSelectedSteamIdentity>
                {
                    new MatchSelectedSteamIdentity
                    {
                        SteamIdentityId = steamIdentity1.Id,
                        SteamIdentity = steamIdentity1,
                        matchId = match.Id,
                    },
                    new MatchSelectedSteamIdentity
                    {
                        SteamIdentityId = steamIdentity2.Id,
                        SteamIdentity = steamIdentity2,
                        matchId = match.Id
                    }
                };
                
                matchSelectedSteamIdentitiesService.Upsert(match.MatchSelectedSteamIdentities,match.Id).GetAwaiter().GetResult();
            }

            matchService.Upsert(match);
            return match;
        }
        [Fact]
        public void DefaultDBTeam()
        {
            DefaultDB(true);
        }
        [Fact]
        public void DefaultDBNoTeam()
        {
            DefaultDB(false);
        }
        
        public void DefaultDB(bool teamMatch)
        {
            // arrange
            var pavlovServers = PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            pavlovServers.First().ServerType = ServerType.Event;
            _pavlovServerService.Upsert(pavlovServers.First());
            var tmpMatch = CreateMatch(_matchService, pavlovServers.First(), teamMatch, _teamService, _steamIdentityService, _userManager,_matchSelectedSteamIdentitiesService,_matchSelectedTeamSteamIdentitiesService);

            // act
            var result = _matchService.FindOne(tmpMatch.Id).GetAwaiter().GetResult();
            var result2 = _matchService.FindAll().GetAwaiter().GetResult();
            result.Status = Status.Finshed;
            _matchService.Upsert(result).GetAwaiter().GetResult();
            
            if (teamMatch)
            {
                
                var matchTeamSelectedSteamIdentities = _matchSelectedTeamSteamIdentitiesService.FindAll().GetAwaiter().GetResult();
                //cause i added 2
                matchTeamSelectedSteamIdentities.Should().HaveCount(2);
                
                var selected = _matchSelectedTeamSteamIdentitiesService.FindAllSelectedForMatchAndTeam(tmpMatch.Id,
                    matchTeamSelectedSteamIdentities.First().TeamId).GetAwaiter().GetResult();
                //cause i selected one per team
                selected.Should().HaveCount(1);
                _matchSelectedTeamSteamIdentitiesService.RemoveFromMatch(matchTeamSelectedSteamIdentities.First().Id)
                    .GetAwaiter().GetResult();
                
                matchTeamSelectedSteamIdentities = _matchSelectedTeamSteamIdentitiesService.FindAll().GetAwaiter().GetResult();
                matchTeamSelectedSteamIdentities.Should().BeNullOrEmpty();
                
                
            }
            else
            {
                var matchSelectedSteamIdentities = _matchSelectedSteamIdentitiesService.FindAll().GetAwaiter().GetResult();
                matchSelectedSteamIdentities.Should().HaveCount(2);

                
                matchSelectedSteamIdentities = _matchSelectedSteamIdentitiesService.FindAllSelectedForMatch(tmpMatch.Id)
                    .GetAwaiter().GetResult();
                
                matchSelectedSteamIdentities.Should().HaveCount(2);


                _matchSelectedSteamIdentitiesService.RemoveFromMatch(tmpMatch.Id).GetAwaiter().GetResult();
                
                matchSelectedSteamIdentities = _matchSelectedSteamIdentitiesService.FindAll().GetAwaiter().GetResult();
                //cause i removed 1
                matchSelectedSteamIdentities.Should().BeNullOrEmpty();
            }
            
            
            var result3 = _matchService.CanBeDeleted(tmpMatch.Id).GetAwaiter().GetResult();
            var result4 = _matchService.Delete(tmpMatch.Id).GetAwaiter().GetResult();
            
            var result5 = _matchService.FindOne(tmpMatch.Id).GetAwaiter().GetResult();
            
            // assert
            result.Should().NotBeNull();
            result2.Should().HaveCount(1);
            result3.Should().BeTrue();
            result4.Should().BeTrue();
            result5.Should().BeNull();
            
            
        }
        

        
        

    }
}