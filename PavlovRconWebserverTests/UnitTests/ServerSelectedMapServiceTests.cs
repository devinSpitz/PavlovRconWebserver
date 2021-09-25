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
    public class ServerSelectedMapServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly ServerSelectedMapService _serverSelectedMapService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly PavlovServerService _pavlovServerService;
        private readonly MapsService _mapsService;
        public ServerSelectedMapServiceTests() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _serverSelectedMapService = _mocker.CreateInstance<ServerSelectedMapService>();
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
            _mapsService = _mocker.CreateInstance<MapsService>();
        }

        private PavlovServer[] InitializeServerSelectetMapUpsert()
        {
            var maps = InitializeServerSelectetMap(out var pavlovServers);
            _serverSelectedMapService.Upsert(new List<ServerSelectedMap>()
            {
                new ServerSelectedMap()
                {
                    GameMode = "TDM",
                    Id = 1,
                    Map = maps.First(),
                    PavlovServer = pavlovServers.First()
                }
            }).GetAwaiter().GetResult();
            return pavlovServers;
        }
        
        private Map[] InitializeServerSelectetMap(out PavlovServer[] pavlovServers)
        {
            InitializePavlovServerAndSShServerAndMap(_sshServerSerivce, _pavlovServerService, _mapsService);
            var maps = _mapsService.FindAll().GetAwaiter().GetResult();
            pavlovServers = _pavlovServerService.FindAll().GetAwaiter().GetResult();
            maps.Should().NotBeNullOrEmpty();
            pavlovServers.Should().NotBeNullOrEmpty();
            return maps;
        }

        private static void InitializePavlovServerAndSShServerAndMap(SshServerSerivce sshServerSerivce,PavlovServerService pavlovServerService,MapsService mapsService)
        {
            var sshServer = SshServerServiceTests.SshServerInsert(sshServerSerivce);
            PavlovServerServiceTests.PavlovServers(sshServer,pavlovServerService);
            MapServiceTests.InsertMap(mapsService);
        }
        
        [Fact]
        public void InsertMap()
        {
            // arrange
            var maps = InitializeServerSelectetMap(out var pavlovServers);
            // act
            _serverSelectedMapService.Insert(new ServerSelectedMap()
            {
                GameMode = "TDM",
                Id = 1,
                Map = maps.First(),
                PavlovServer = pavlovServers.First()
            }).GetAwaiter().GetResult();
            // assert
            var mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().HaveCount(1);
        }




        [Fact]
        public void DeleteMap()
        {
            // arrange
            var pavlovServers = InitializeServerSelectetMapUpsert();
            var mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().HaveCount(1);
            // act
            _serverSelectedMapService.Delete(mapsResult.First().Id);
            // assert
            mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().BeEmpty();
        }
        
        [Fact]
        public void DeleteMapFromServer()
        {
            // arrange
            var pavlovServers = InitializeServerSelectetMapUpsert();
            var mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().HaveCount(1);
            // act
            var result = _serverSelectedMapService.DeleteFromServer(pavlovServers.First()).GetAwaiter().GetResult();
            // assert
            mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().BeEmpty();
        }
        
        [Fact]
        public void UpdateMap()
        {
            // arrange
            var pavlovServers = InitializeServerSelectetMapUpsert();
            var mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().HaveCount(1);
            // act
            var map = mapsResult.First();
            map.GameMode = "GUN";
            _serverSelectedMapService.Update(map);
            // assert
            mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
            mapsResult.Should().HaveCount(1);
            var mapResult = mapsResult.First();
            mapResult.GameMode.Should().Be("GUN");
        }
        
        // [Fact]
        // public void FindOne()
        // {
        //     // arrange        var pavlovServers = InitializeServerSelectetMap();
        //     var mapsResult = _serverSelectedMapService.FindAllFrom(pavlovServers.First()).GetAwaiter().GetResult();
        //     mapsResult.Should().HaveCount(1);
        //     // act
        //     var map = _serverSelectedMapService.F("1").GetAwaiter().GetResult();
        //     // assert
        //     map.Should().NotBe(null);
        //     map.Name.Should().Be("test");
        // }

    }
}