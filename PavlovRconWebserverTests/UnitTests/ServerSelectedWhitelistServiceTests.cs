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
    public class ServerSelectedWhitelistServiceTests
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly ServerSelectedWhitelistService _serverSelectedWhitelistService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly UserManager<LiteDbUser> _userManager;
        private readonly IServicesBuilder services;

        public ServerSelectedWhitelistServiceTests()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _serverSelectedWhitelistService = _mocker.CreateInstance<ServerSelectedWhitelistService>();
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _userManager = services.GetUserManager();
        }


        private SteamIdentity InitializeEntryForWhiteList(out PavlovServer[] pavlovServers)
        {
            var user = ServerSelectedModServiceTests.InsertUserAndSteamIdentity(_steamIdentityService, _userManager);
            var steamIdentity = _steamIdentityService.FindOne(user.Id).GetAwaiter().GetResult();
            pavlovServers = PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            return steamIdentity;
        }

        [Fact]
        public void InsertWhitelist()
        {
            // arrange
            var steamIdentity = InitializeEntryForWhiteList(out var pavlovServers);
            // act
            _serverSelectedWhitelistService.Insert(new ServerSelectedWhiteList
            {
                SteamIdentityId = steamIdentity.Id,
                PavlovServer = pavlovServers.First()
            }).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(1);
        }


        [Fact]
        public void DeleteWhitelist()
        {
            // arrange
            var pavlovServers = InitializeEntryForWhitelistAndInsertIt();

            var whitelistsResultBefor = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResultBefor.Should().HaveCount(1);
            // act
            _serverSelectedWhitelistService.Delete(whitelistsResultBefor.First().Id).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(0);
        }

        private PavlovServer[] InitializeEntryForWhitelistAndInsertIt()
        {
            var steamIdentity = InitializeEntryForWhiteList(out var pavlovServers);
            _serverSelectedWhitelistService.Insert(new ServerSelectedWhiteList
            {
                SteamIdentityId = steamIdentity.Id,
                PavlovServer = pavlovServers.First()
            }).GetAwaiter().GetResult();
            return pavlovServers;
        }

        [Fact]
        public void DeleteWhitelistFromServer()
        {
            // arrange
            var pavlovServers = InitializeEntryForWhitelistAndInsertIt();
            _serverSelectedWhitelistService.FindAll().GetAwaiter().GetResult().Should().HaveCount(1);
            // act
            _serverSelectedWhitelistService.DeleteFromServer(pavlovServers.First()).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(0);
        }

        [Fact]
        public void UpdateWhitelist()
        {
            // arrange
            var steamIdentity = InitializeEntryForWhiteList(out var pavlovServers);
            var id = _serverSelectedWhitelistService.Insert(new ServerSelectedWhiteList
            {
                SteamIdentityId = steamIdentity.Id,
                PavlovServer = pavlovServers.First()
            }).GetAwaiter().GetResult();
            var whiteListEntry = _serverSelectedWhitelistService.FindOne(id).GetAwaiter().GetResult();
            whiteListEntry.SteamIdentityId = "123";
            // act
            _serverSelectedWhitelistService.Update(whiteListEntry).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(1);

            whitelistsResult.First().SteamIdentityId.Should().Be("123");
        }

        [Fact]
        public void FindAllFrom()
        {
            // arrange
            var pavlovServers = InitializeEntryForWhitelistAndInsertIt();
            // act
            _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(1);
        }

        [Fact]
        public void FindAllFromString()
        {
            // arrange
            var pavlovServers = InitializeEntryForWhitelistAndInsertIt();
            var steamIdentity = _serverSelectedWhitelistService.FindAll().GetAwaiter().GetResult();
            // act
            _serverSelectedWhitelistService.FindAllFrom(steamIdentity.First().SteamIdentityId).GetAwaiter().GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(1);
        }

        [Fact]
        public void FindSelectedMap()
        {
            // arrange
            var pavlovServers = InitializeEntryForWhitelistAndInsertIt();
            var steamIdentity = _serverSelectedWhitelistService.FindAll().GetAwaiter().GetResult();
            // act
            _serverSelectedWhitelistService
                .FindSelectedMap(pavlovServers.First().Id, steamIdentity.First().SteamIdentityId).GetAwaiter()
                .GetResult();
            // assert
            var whitelistsResult = _serverSelectedWhitelistService.FindAllFrom(pavlovServers.First()).GetAwaiter()
                .GetResult();
            whitelistsResult.Should().HaveCount(1);
        }
    }
}