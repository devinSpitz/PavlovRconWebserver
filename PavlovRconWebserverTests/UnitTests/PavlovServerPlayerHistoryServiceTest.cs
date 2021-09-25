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
    public class PavlovServerPlayerHistoryServiceTest
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly PavlovServerPlayerHistoryService _pavlovServerPlayerHistoryService;
        public PavlovServerPlayerHistoryServiceTest() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerPlayerHistoryService = _mocker.CreateInstance<PavlovServerPlayerHistoryService>();

        }

        public static void CreatePavlovServerHistory(PavlovServerPlayerHistoryService pavlovServerPlayerHistoryService,int pavlovServerId,string playerId)
        {
            pavlovServerPlayerHistoryService.Upsert(new List<PavlovServerPlayerHistory>(){
                new PavlovServerPlayerHistory
                {
                    Username = "null",
                    UniqueId = playerId,
                    PlayerName = "null",
                    KDA = "null",
                    Cash = "null",
                    TeamId = 0,
                    Score = 0,
                    Kills = 0,
                    Deaths = 0,
                    Assists = 0,
                    ServerId = pavlovServerId,
                    Id = null,
                    date = default
                }
                },
                pavlovServerId,1).GetAwaiter().GetResult();
        }
        
        [Fact]
        public void FindServer()
        {
            // arrange
            var pavlovServers = PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
           CreatePavlovServerHistory(_pavlovServerPlayerHistoryService, pavlovServers.First().Id,"test");
            // act
            var serverResult = _pavlovServerPlayerHistoryService.FindAllFromServer(pavlovServers.First().Id).GetAwaiter().GetResult();
            var serverResult2 = _pavlovServerPlayerHistoryService.FindAllFromPlayer("test").GetAwaiter().GetResult();
            // assert
            serverResult.Should().HaveCount(1);
            serverResult2.Should().HaveCount(1);
        }
        
        
        [Fact]
        public void UpsertAfterUpsert()
        {
            // arrange
            var pavlovServers = PavlovServerServiceTests.InitializePavlovServer(_sshServerSerivce, _pavlovServerService);
            CreatePavlovServerHistory(_pavlovServerPlayerHistoryService, pavlovServers.First().Id,"test");
            CreatePavlovServerHistory(_pavlovServerPlayerHistoryService, pavlovServers.First().Id,"test2");
            // act
            var serverResult = _pavlovServerPlayerHistoryService.FindAllFromServer(pavlovServers.First().Id).GetAwaiter().GetResult();
            // assert
            serverResult.Should().HaveCount(1);
        }

        
        

    }
}