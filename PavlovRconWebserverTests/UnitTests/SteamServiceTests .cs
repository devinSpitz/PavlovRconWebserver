using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class SteamServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly SteamService _steamService;
        private readonly MapsService _mapsService;
        public SteamServiceTests() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _steamService = _mocker.CreateInstance<SteamService>();
            _mapsService = _mocker.CreateInstance<MapsService>();
        }
        
        
        
        [Fact]
        public void CrawlTest()
        {
            // arrange
            _steamService.CrawlSteamMaps().GetAwaiter().GetResult();
            // act
            // assert
            var maps =_mapsService.FindAll().GetAwaiter().GetResult();
            maps.Where(x=>x.Name!="Vankrupt Games").ToArray().Length.Should().BeGreaterThan(1);
        }

        
    }
}