using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using Moq.AutoMock;
using PavlovRconWebserver.Models;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class PavlovServerServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly PavlovServerService _pavlovServerService;
        private readonly SshServerSerivce _sshServerSerivce;
        private readonly SteamIdentityService _steamIdentityService;
        private readonly ServerSelectedModsService _serverSelectedModsService;
        private readonly UserManager<LiteDbUser> _userManager;
        
        public PavlovServerServiceTests() {
            
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _userManager = services.GetUserManager();
                
            // Mock ValidatePavlovServer function
            var tmpService = _mocker.Setup<PavlovServerService,Task<PavlovServer>>(x => x.ValidatePavlovServer(It.IsAny<PavlovServer>(),It.IsAny<bool>())).ReturnsAsync((PavlovServer server) => server);
            _mocker.Use(tmpService);
            _pavlovServerService = _mocker.GetMock<PavlovServerService>().Object;
            
            
            _sshServerSerivce = _mocker.CreateInstance<SshServerSerivce>();
            _serverSelectedModsService = _mocker.CreateInstance<ServerSelectedModsService>();
            _steamIdentityService = _mocker.CreateInstance<SteamIdentityService>();
        }

        public static PavlovServer[] InitializePavlovServer(SshServerSerivce sshServerSerivce,PavlovServerService pavlovServerService)
        {
            var sshServer = SshServerServiceTests.SshServerInsert(sshServerSerivce);
             return PavlovServers(sshServer,pavlovServerService);
        }

        
        [Fact]
        public void IsModSomeWhere()
        {
            // arrange
            var pavlovServers = InitializePavlovServer(_sshServerSerivce,_pavlovServerService);
            var user = UserServiceTests.SetUpUser(_userManager);
            var steamIdentity = SteamIdentityServiceTest.SteamIdentity();
            steamIdentity.LiteDbUser = user;
            steamIdentity.LiteDbUserId = user.Id.ToString();
            _steamIdentityService.Upsert(steamIdentity).GetAwaiter().GetResult();
            _serverSelectedModsService.Insert(new ServerSelectedMods()
            {
                PavlovServer = pavlovServers.First(),
                LiteDbUser = user
            }).GetAwaiter().GetResult();
            // act
            var isMod = _pavlovServerService.IsModSomeWhere(user, _serverSelectedModsService).GetAwaiter().GetResult();
            // assert
            isMod.Should().BeTrue();
        }
        [Fact]
        public void InsertPavlovServer()
        {
            // arrange
            var sshServer = SshServerServiceTests.SshServerInsert(_sshServerSerivce);
            // act
            _pavlovServerService.Upsert(PavlovServer(sshServer)).GetAwaiter().GetResult();
            // assert
            var pavlovServers =_pavlovServerService.FindAll().GetAwaiter().GetResult();
            pavlovServers.Should().HaveCount(1);
        }
        
        [Fact]
        public void IsValidOnly()
        {
            // arrange
            var sshServer = SshServerServiceTests.SshServerInsert(_sshServerSerivce);
            var pavlovServer = PavlovServer(sshServer);
            _pavlovServerService.Upsert(pavlovServer).GetAwaiter().GetResult();
            // act
            _pavlovServerService.IsValidOnly(pavlovServer).GetAwaiter().GetResult();
            // assert
            var pavlovServers =_pavlovServerService.FindAll().GetAwaiter().GetResult();
            pavlovServers.Should().HaveCount(1);
        }

 

        private static PavlovServer PavlovServer(SshServer sshServer)
        {
            return new PavlovServer
            {
                Name = "test",
                TelnetPort = 9100,
                DeletAfter = 7,
                TelnetPassword = "test",
                ServerPort = 9101,
                ServerFolderPath = "/home/steam/pavlovserver",
                ServerSystemdServiceName = "pavlovserver",
                ServerType = ServerType.Community,
                ServerServiceState = ServerServiceState.inactive,
                SshServer = sshServer
            };
        }

        [Fact]
        public void DeletePavlovServer()
        {
            // arrange
            var sshServer = SshServerServiceTests.SshServerInsert(_sshServerSerivce);
            var pavlovServers = PavlovServers(sshServer,_pavlovServerService);
            pavlovServers.Should().NotBeNullOrEmpty();
            // act
            _pavlovServerService.Delete(pavlovServers.FirstOrDefault().Id).GetAwaiter().GetResult();
            // assert
            var pavlovServersAfterDelete =_pavlovServerService.FindAll().GetAwaiter().GetResult();
            pavlovServersAfterDelete.Should().BeEmpty();
        }

        [Fact]
        public void FindOne()
        {
            // arrange
            var sshServer = SshServerServiceTests.SshServerInsert(_sshServerSerivce);
            var pavlovServers = PavlovServers(sshServer,_pavlovServerService);
            pavlovServers.Should().NotBeNullOrEmpty();
            // act
            var pavlovServer = _pavlovServerService.FindOne(pavlovServers.FirstOrDefault().Id).GetAwaiter().GetResult();
            // assert
            pavlovServer.Should().NotBe(null);
            pavlovServer.Name.Should().Be("test");
        }

        public static PavlovServer[] PavlovServers(SshServer sshServer,PavlovServerService pavlovServerService)
        {
            pavlovServerService.Upsert(PavlovServer(sshServer), false);
            var pavlovServers = pavlovServerService.FindAll().GetAwaiter().GetResult();
            return pavlovServers;
        }
    }
}