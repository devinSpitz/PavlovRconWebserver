using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class MapServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly MapsService _mapsService;
        public MapServiceTests() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _mapsService = _mocker.CreateInstance<MapsService>();
        }
        
        [Fact]
        public void InsertMapTest()
        {
            // arrange

            // act
            InsertMap(_mapsService);
            // assert
            var maps =_mapsService.FindAll().GetAwaiter().GetResult();
            maps.Should().HaveCount(1);
        }

        public static void InsertMap(MapsService mapsService)
        {
            mapsService.Upsert(Map()).GetAwaiter().GetResult();
        }

        [Fact]
        public void DeleteMap()
        {
            // arrange
            InsertMap(_mapsService);
            // act
            _mapsService.Delete("1");
            // assert
            var maps =_mapsService.FindAll().GetAwaiter().GetResult();
            maps.Should().BeEmpty();
        }

        [Fact]
        public void FindOne()
        {
            // arrange
            InsertMap(_mapsService);
            // act
            var map = _mapsService.FindOne("1").GetAwaiter().GetResult();
            // assert
            map.Should().NotBe(null);
            map.Name.Should().Be("test");
        }

        private static Map Map()
        {
            return new Map()
            {
                Author = "test",
                ImageUrl = "",
                Name = "test",
                Id = "1"
            };
        }
    }
}