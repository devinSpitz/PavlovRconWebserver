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
    public class TeamSelectedSteamIdentityServiceTest
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly TeamService _teamService;
        private readonly TeamSelectedSteamIdentityService _teamSelectedSteamIdentityService;
        public TeamSelectedSteamIdentityServiceTest() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _userManager = services.GetUserManager();
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
            _teamSelectedSteamIdentityService = _mocker.CreateInstance<TeamSelectedSteamIdentityService>();
            _teamService = _mocker.CreateInstance<TeamService>();

        }

        public static Team CreateTeam(TeamService teamService,string name = "test")
        {
            teamService.Upsert(new Team()
            {
                Name = name
            }).GetAwaiter().GetResult();
            return teamService.FindAll().GetAwaiter().GetResult().FirstOrDefault(x => x.Name == name);
        }
        
        private void CreateAndInitializeATeamWithAMember(out LiteDbUser user, out SteamIdentity steamIdentity, out Team team)
        {
            user = ServerSelectedModServiceTests.InsertUserAndSteamIdentity(_steamIdentityService, _userManager);
            steamIdentity = _steamIdentityService.FindAll().GetAwaiter().GetResult().First();
            team = CreateTeam(_teamService);
            _teamSelectedSteamIdentityService.Insert(new TeamSelectedSteamIdentity()
            {
                Team = team,
                SteamIdentity = steamIdentity,
                RoleOverwrite = ""
            });
        }
        [Fact]
        public void FindAllFrom()
        {
            // arrange
            CreateAndInitializeATeamWithAMember(out var user, out var steamIdentity, out var team);
            // act
            var result = _teamSelectedSteamIdentityService.FindAllFrom(steamIdentity).GetAwaiter().GetResult();
            var result2 = _teamSelectedSteamIdentityService.FindAllFrom(team.Id).GetAwaiter().GetResult();
            // assert
            result.Should().HaveCount(1);
            result2.Should().HaveCount(1);

        }


        [Fact]
        public void FindOne()
        {           
            // arrange
            CreateAndInitializeATeamWithAMember(out var user, out var steamIdentity, out var team);
            // act
            var result = _teamSelectedSteamIdentityService.FindOne(steamIdentity.Id).GetAwaiter().GetResult();
            var result2 = _teamSelectedSteamIdentityService.FindOne(team.Id).GetAwaiter().GetResult();
            var result3 = _teamSelectedSteamIdentityService.FindOne(team.Id,steamIdentity.Id).GetAwaiter().GetResult();
            // assert
            result.Should().NotBeNull();
            result2.Should().NotBeNull();
            result3.Should().NotBeNull();
        }
        
        [Fact]
        public void Update()
        {
            // arrange
            CreateAndInitializeATeamWithAMember(out var user, out var steamIdentity, out var team);
            var teamSelectedSteamIdentity = _teamSelectedSteamIdentityService.FindOne(steamIdentity.Id).GetAwaiter().GetResult();
            // act
            teamSelectedSteamIdentity.RoleOverwrite = "Admin";
            var result =  _teamSelectedSteamIdentityService.Update(teamSelectedSteamIdentity).GetAwaiter().GetResult();
            
            teamSelectedSteamIdentity = _teamSelectedSteamIdentityService.FindOne(teamSelectedSteamIdentity.Id).GetAwaiter().GetResult();
            // assert
            teamSelectedSteamIdentity.RoleOverwrite.Should().Be("Admin");
        }
        
        [Fact]
        public void Delete()
        {            
            CreateAndInitializeATeamWithAMember(out var user, out var steamIdentity, out var team);
            var teamSelectedSteamIdentity = _teamSelectedSteamIdentityService.FindOne(steamIdentity.Id).GetAwaiter().GetResult();
            // act
            var result =  _teamSelectedSteamIdentityService.Delete(teamSelectedSteamIdentity.Id).GetAwaiter().GetResult();
            
            teamSelectedSteamIdentity = _teamSelectedSteamIdentityService.FindOne(teamSelectedSteamIdentity.Id).GetAwaiter().GetResult();
            // assert
            teamSelectedSteamIdentity.Should().BeNull();
            result.Should().BeTrue();
        }
        
        

    }
}