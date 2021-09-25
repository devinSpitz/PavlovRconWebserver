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
    public class SteamIdentityServiceTest
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly SteamIdentityService _steamIdentityService;
        public SteamIdentityServiceTest() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
        }
        
        [Fact]
        public void InsertSteamIdentity()
        {
            // arrange

            // act
            var result = _steamIdentityService.Upsert(SteamIdentity()).GetAwaiter().GetResult();
            // assert
            var steamIdentities =_steamIdentityService.FindAll().GetAwaiter().GetResult();
            steamIdentities.Should().HaveCount(1);
        }

        public static SteamIdentity SteamIdentity(string id = "1",string name = "test")
        {
            return new SteamIdentity()
            {
                Id = id,
                Name = name
            };
        }

        public static List<SteamIdentity> SteamIdentities()
        {
            return new List<SteamIdentity>
            {
                SteamIdentity(),
                SteamIdentity("2","test2"),
                SteamIdentity("3","test3"),
                SteamIdentity("4","test4"),
                SteamIdentity("5","test5"),
            };
        }
        public static List<SteamIdentity> SteamIdentitiesForTeam2()
        {
            return new List<SteamIdentity>
            {
                SteamIdentity(),
                SteamIdentity("6","test6"),
                SteamIdentity("7","test7"),
                SteamIdentity("8","test8"),
                SteamIdentity("9","test9"),
            };
        }
        
        [Fact]
        public void DeleteSteamIdentity()
        {
            // arrange
            _steamIdentityService.Upsert(SteamIdentity());
            // act
            _steamIdentityService.Delete("1");
            // assert
            var steamIdentity =_steamIdentityService.FindAll().GetAwaiter().GetResult();
            steamIdentity.Should().BeEmpty();
        }

        [Fact]
        public void FindOneSteamIdentity()
        {
            // arrange
            _steamIdentityService.Upsert(SteamIdentity()).GetAwaiter().GetResult();
            // act
            var steamIdentity = _steamIdentityService.FindOne("1").GetAwaiter().GetResult();
            // assert
            steamIdentity.Should().NotBe(null);
            steamIdentity.Name.Should().Be("test");
        }
        

        [Fact]
        public void FindOneAList()
        {
            // arrange
            _steamIdentityService.Upsert(SteamIdentity()).GetAwaiter().GetResult();
            // act
            var steamIdentity = _steamIdentityService.FindAList(new List<string>(){SteamIdentity().Id}).GetAwaiter().GetResult();
            // assert
            steamIdentity.Should().NotBeEmpty(null);
            steamIdentity.FirstOrDefault()?.Name.Should().Be("test");
        }
        
        [Fact]
        public void FindOne()
        {
            // arrange
            _steamIdentityService.Upsert(SteamIdentity()).GetAwaiter().GetResult();
            // act
            var steamIdentity = _steamIdentityService.FindOne(SteamIdentity().Id).GetAwaiter().GetResult();
            // assert
            steamIdentity.Should().NotBeNull();
            steamIdentity.Name.Should().Be("test");
        }

        
    }
}