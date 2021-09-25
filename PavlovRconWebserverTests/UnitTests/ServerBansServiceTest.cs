using System;
using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class ServerBansServiceTest
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly ServerBansService _serverBansService;
        public ServerBansServiceTest() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _serverBansService = _mocker.CreateInstance<ServerBansService>();

        }

        public static ServerBans CreatePavlovServerBans(ServerBansService serverBansService,PavlovServer pavlovServer)
        {
            serverBansService.Upsert(
                new ServerBans
                {
                    SteamId = "test",
                    SteamName = "test",
                    BannedDateTime = DateTime.Now,
                    BanSpan = new TimeSpan(0,0,1),
                    Comment = "test",
                    PavlovServer = pavlovServer
                }).GetAwaiter().GetResult();
            return serverBansService.FindOne(pavlovServer.Id).GetAwaiter().GetResult();
        }
        
        [Fact]
        public void FindAllFromServer()
        {
            // arrange
            var pavlovServers = PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            var serverBan = CreatePavlovServerBans(_serverBansService, pavlovServers.First());
            // act
            var serverResult = _serverBansService.FindOne(serverBan.Id).GetAwaiter().GetResult();
            var serverResult1 = _serverBansService.FindAll().GetAwaiter().GetResult();
            var serverResult2 = _serverBansService.FindAllFromPavlovServerId(pavlovServers.First().Id,false).GetAwaiter().GetResult();
            var serverResult3 = _serverBansService.FindAllFromPavlovServerId(pavlovServers.First().Id,true).GetAwaiter().GetResult();
            var serverResult4 = _serverBansService.Delete(serverBan.Id).GetAwaiter().GetResult();
            var serverResult5 = _serverBansService.FindOne(serverBan.Id).GetAwaiter().GetResult();
            // assert
            serverResult.Should().NotBeNull();
            serverResult1.Should().HaveCount(1);
            serverResult2.Should().HaveCount(1);
            serverResult3.Should().HaveCount(1);
            serverResult4.Should().BeTrue();
            serverResult5.Should().BeNull();
        }
        

        
        

    }
}