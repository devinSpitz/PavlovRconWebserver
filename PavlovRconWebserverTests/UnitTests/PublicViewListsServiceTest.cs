using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class PublicViewListsServiceTest
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerInfoService _pavlovServerInfoService;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly PublicViewListsService _publicViewListsService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly IServicesBuilder services;

        public PublicViewListsServiceTest()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerInfoService = _mocker.CreateInstance<PavlovServerInfoService>();
            _pavlovServerPlayerService = _mocker.CreateInstance<PavlovServerPlayerService>();
            _publicViewListsService = _mocker.CreateInstance<PublicViewListsService>();
        }

        [Fact]
        public void ViewModelTest()
        {
            // arrange
            var pavlovServers =
                PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            var pavlovServerInfo =
                PavlovServerInfoServiceTest.CreatePavlovServerInfo(_pavlovServerInfoService, pavlovServers.First().Id);
            var pavlovServerPlayer =
                PavlovServerPlayerServiceTest.CreatePavlovServerPlayer(_pavlovServerPlayerService,
                    pavlovServers.First().Id);

            // act
            var result =
                _publicViewListsService.PavlovServerPlayerListPublicViewModel(pavlovServerInfo,
                    new[] {pavlovServerPlayer});
            var result2 = _publicViewListsService.GetPavlovServerPlayerListPublicViewModel(pavlovServers.First().Id)
                .GetAwaiter().GetResult();
            // assert
            result.ServerInfo.ServerName.Should().Be("test");
            result2.PlayerList.Should().NotBeNull();
            result.PlayerList.Should().NotBeNull();
            result.Should().NotBeNull();
            result2.Should().NotBeNull();
        }
    }
}