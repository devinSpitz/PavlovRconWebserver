using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq.AutoMock;
using PavlovRconWebserver.Exceptions;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class SshServerServiceTests
    {
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly IServicesBuilder services;

        public SshServerServiceTests()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _pavlovServerService = _mocker.CreateInstance<PavlovServerService>();
        }


        [Fact]
        public void InsertSshServer()
        {
            // arrange
            // act
            var result = _sshServerSerivce.Insert(SshServer(),false).GetAwaiter().GetResult();
            // assert
            var SshServers = _sshServerSerivce.FindAll().GetAwaiter().GetResult();
            SshServers.Should().HaveCount(1);
        }

        public static SshServer SshServer(List<PavlovServer> pavlovServers = null)
        {
            return new()
            {
                Adress = "localhost",
                SshUsername = "steam",
                SshPassword = "1234",
                Name = "test",
                PavlovServers = pavlovServers ?? new List<PavlovServer>()
            };
        }

        public static SshServer SshServerInsert(SshServerSerivce sshServerSerivce)
        {
            sshServerSerivce.Insert(SshServer(),false).GetAwaiter().GetResult();
            var sshServer = sshServerSerivce.FindAll().GetAwaiter().GetResult().FirstOrDefault();
            return sshServer;
        }

        [Fact]
        public void DeleteSshServer()
        {
            // arrange
            var sshServer = SshServerInsert(_sshServerSerivce);
            sshServer.PavlovServers = PavlovServerServiceTests.PavlovServers(sshServer, _pavlovServerService).ToList();
            sshServer.Should().NotBeNull();
            // act
            _sshServerSerivce.Delete(sshServer.Id).GetAwaiter().GetResult();
            // assert
            var SshServers = _sshServerSerivce.FindAll().GetAwaiter().GetResult();
            SshServers.Should().BeEmpty();
        }

        [Fact]
        public void FindOne()
        {
            // arrange
            var sshServer = SshServerInsert(_sshServerSerivce);
            sshServer.Should().NotBeNull();
            // act
            var sshServerResult = _sshServerSerivce.FindOne(sshServer.Id).GetAwaiter().GetResult();
            // assert
            sshServerResult.Should().NotBe(null);
            sshServerResult.Name.Should().Be("test");
        }


        [Fact]
        public void Update()
        {
            // arrange
            var sshServer = SshServerInsert(_sshServerSerivce);
            var sshServers = _sshServerSerivce.FindAll().GetAwaiter().GetResult();
            sshServer.Should().NotBeNull();
            // act
            sshServer.Name = "UpdateTest";
            sshServer.SshPassword = "";
            sshServer.SshPassphrase = "";
            var sshServerResult = _sshServerSerivce.Update(sshServer,false).GetAwaiter().GetResult();
            // assert

            var sshServersAfterUpdate = _sshServerSerivce.FindAll().GetAwaiter().GetResult();
            var sshServerAfterUpdate = sshServersAfterUpdate.FirstOrDefault();
            sshServerAfterUpdate.Should().NotBeNull();
            sshServerAfterUpdate.Name.Should().Be("UpdateTest");
            sshServerResult.Should().BeTrue();
        }

        [Fact]
        public void InsertNoPassword()
        {
            // arrange
            var tmp = SshServer();
            tmp.SshPassword = "";

            try
            {
                _sshServerSerivce.Insert(tmp).GetAwaiter().GetResult();
            }
            catch (ValidateException e)
            {
                Assert.Equal("You need at least a password or a key file!", e.Message);
            }
        }

        [Fact]
        public void InsertNoUserName()
        {
            // arrange
            var tmp = SshServer();
            tmp.SshUsername = "";

            try
            {
                _sshServerSerivce.Insert(tmp).GetAwaiter().GetResult();
            }
            catch (ValidateException e)
            {
                Assert.Equal("You need a username!", e.Message);
            }
        }

        [Fact]
        public void InsertNoSsPort()
        {
            // arrange
            var tmp = SshServer();
            tmp.SshPort = 0;

            try
            {
                _sshServerSerivce.Insert(tmp).GetAwaiter().GetResult();
            }
            catch (ValidateException e)
            {
                Assert.Equal("You need a SSH port!", e.Message);
            }
        }
    }
}