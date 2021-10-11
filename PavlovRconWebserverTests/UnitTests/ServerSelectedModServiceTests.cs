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
    public class ServerSelectedModServiceTests
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly IServicesBuilder services;

        public ServerSelectedModServiceTests()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _serverSelectedModsService = _mocker.CreateInstance<ServerSelectedModsService>();
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _userManager = services.GetUserManager();
        }

        public static LiteDbUser InsertUserAndSteamIdentity(SteamIdentityService steamIdentityService,
            UserManager<LiteDbUser> userManager, string userName = "Test", string steamIdentityId = "1")
        {
            var user = UserServiceTests.SetUpUser(userManager, userName);
            var identity = SteamIdentityServiceTest.SteamIdentity(steamIdentityId);
            identity.LiteDbUser = user;
            identity.LiteDbUserId = user.Id.ToString();
            steamIdentityService.Upsert(identity).GetAwaiter().GetResult();
            return user;
        }

        private static PavlovServer[] InsertToPavlovServer(LiteDbUser user, SshServerSerivce sshServerSerivce,
            PavlovServerService pavlovServerService, ServerSelectedModsService serverSelectedModsService,
            bool withInsert = true)
        {
            var pavlovServers =
                PavlovServerServiceTests.InitializePavlovServer(sshServerSerivce, pavlovServerService);

            if (withInsert)
                serverSelectedModsService.Insert(new ServerSelectedMods
                {
                    LiteDbUser = user,
                    PavlovServer = pavlovServers.First()
                }).GetAwaiter().GetResult();
            return pavlovServers;
        }

        [Fact]
        public void SteamIdentitiesToReturn()
        {
            // arrange
            var user = InsertUserAndSteamIdentity(_steamIdentityService, _userManager);
            var pavlovServers = InsertToPavlovServer(user, _sshServerSerivce, _pavlovServerService,
                _serverSelectedModsService, false);
            // act
            var steamIdentities = _serverSelectedModsService
                .SteamIdentitiesToReturn(new List<string> {user.Id.ToString()}, pavlovServers.First(),
                    _steamIdentityService.FindAll().GetAwaiter().GetResult().ToList()).GetAwaiter().GetResult();
            var steamIdentitiesInserted =
                _serverSelectedModsService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            // assert
            steamIdentities.Should().HaveCount(1);
            steamIdentitiesInserted.Should().HaveCount(1);
        }

        [Fact]
        public void SteamIdentitiesToReturnWithoutInsert()
        {
            // arrange
            var user = InsertUserAndSteamIdentity(_steamIdentityService, _userManager);
            var pavlovServers = InsertToPavlovServer(user, _sshServerSerivce, _pavlovServerService,
                _serverSelectedModsService, false);
            // act
            var steamIdentities = _serverSelectedModsService
                .SteamIdentitiesToReturn(new List<string> {user.Id.ToString()}, pavlovServers.First(),
                    _steamIdentityService.FindAll().GetAwaiter().GetResult().ToList(), false).GetAwaiter().GetResult();
            var steamIdentitiesInserted =
                _serverSelectedModsService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            // assert
            steamIdentities.Should().HaveCount(1);
            steamIdentitiesInserted.Should().BeNullOrEmpty();
        }

        [Fact]
        public void FindAll()
        {
            // arrange
            InsertToPavlovServer(InsertUserAndSteamIdentity(_steamIdentityService, _userManager), _sshServerSerivce,
                _pavlovServerService, _serverSelectedModsService);
            // act
            var steamIdentities = _serverSelectedModsService.FindAll().GetAwaiter().GetResult();
            // assert
            steamIdentities.Should().HaveCount(1);
        }


        [Fact]
        public void Delete()
        {
            // arrange
            var user = InsertUserAndSteamIdentity(_steamIdentityService, _userManager);
            InsertToPavlovServer(user, _sshServerSerivce, _pavlovServerService, _serverSelectedModsService);

            var steamIdentities = _serverSelectedModsService.FindAll().GetAwaiter().GetResult();

            // act
            var result = _serverSelectedModsService.Delete(steamIdentities.First().Id).GetAwaiter().GetResult();
            steamIdentities = _serverSelectedModsService.FindAll().GetAwaiter().GetResult();
            // assert
            result.Should().BeTrue();
            steamIdentities.Should().BeNullOrEmpty();
        }
    }
}