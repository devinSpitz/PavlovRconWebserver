using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class PavlovServerInfoServiceTest
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly IServicesBuilder services;

        public PavlovServerInfoServiceTest()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerInfoService = _mocker.CreateInstance<PavlovServerInfoService>();
        }

        public static PavlovServerInfo CreatePavlovServerInfo(PavlovServerInfoService pavlovServerInfoService,
            int pavlovServerId)
        {
            pavlovServerInfoService.Upsert(new PavlovServerInfo
            {
                GameMode = "TDM",
                MapLabel = "test",
                MapPictureLink = "",
                PlayerCount = "test",
                RoundState = "test",
                ServerId = pavlovServerId,
                ServerName = "test",
                Teams = "1",
                Team0Score = "0",
                Team1Score = "0"
            }).GetAwaiter().GetResult();
            return pavlovServerInfoService.FindServer(pavlovServerId).GetAwaiter().GetResult();
        }

        [Fact]
        public void FindServer()
        {
            // arrange
            var pavlovServers =
                PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            CreatePavlovServerInfo(_pavlovServerInfoService, pavlovServers.First().Id);
            // act
            var serverInfoResult =
                _pavlovServerInfoService.FindServer(pavlovServers.First().Id).GetAwaiter().GetResult();
            // assert
            serverInfoResult.Should().NotBeNull();
        }
    }
}