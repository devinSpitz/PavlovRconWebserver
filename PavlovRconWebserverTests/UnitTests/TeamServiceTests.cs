using Moq.AutoMock;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;

namespace PavlovRconWebserverTests.UnitTests
{
    public class TeamServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly TeamService _teamService;
        public TeamServiceTests() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _teamService = _mocker.CreateInstance<TeamService>();
        }
        
        

    }
}