using Moq.AutoMock;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;

namespace PavlovRconWebserverTests.UnitTests
{
    public class TeamServiceTests
    {
        private readonly AutoMocker _mocker;
        private readonly TeamService _teamService;
        private readonly IServicesBuilder services;

        public TeamServiceTests()
        {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);
            _teamService = _mocker.CreateInstance<TeamService>();
        }
    }
}