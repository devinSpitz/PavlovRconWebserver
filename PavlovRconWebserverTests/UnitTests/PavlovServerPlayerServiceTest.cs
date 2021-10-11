using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class PavlovServerPlayerServiceTest
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerPlayerService _pavlovServerPlayerService;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly IServicesBuilder services;

        public PavlovServerPlayerServiceTest()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerPlayerService = _mocker.CreateInstance<PavlovServerPlayerService>();
        }

        public static PavlovServerPlayer CreatePavlovServerPlayer(PavlovServerPlayerService pavlovServerPlayerService,
            int pavlovServerId)
        {
            pavlovServerPlayerService.Upsert(new List<PavlovServerPlayer>
            {
                new()
                {
                    Username = "null",
                    UniqueId = "null",
                    PlayerName = "null",
                    KDA = "1/1/1",
                    Cash = "null",
                    TeamId = 0,
                    Score = 0,
                    Kills = 0,
                    Deaths = 0,
                    Assists = 0,
                    ServerId = pavlovServerId
                }
            }, pavlovServerId).GetAwaiter().GetResult();
            return pavlovServerPlayerService.FindAllFromServer(pavlovServerId).GetAwaiter().GetResult()
                .FirstOrDefault();
        }

        [Fact]
        public void FindAllFromServer()
        {
            // arrange
            var pavlovServers =
                PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            CreatePavlovServerPlayer(_pavlovServerPlayerService, pavlovServers.First().Id);
            // act
            var serverResult = _pavlovServerPlayerService.FindAllFromServer(pavlovServers.First().Id).GetAwaiter()
                .GetResult();
            // assert
            serverResult.Should().HaveCount(1);
        }
    }
}