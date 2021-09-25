using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Moq.AutoMock;

namespace PavlovRconWebserverTests.Mocks
{
    internal interface IServicesBuilder
    {
        void Build(AutoMocker mocker);
        UserManager<LiteDbUser> GetUserManager();
        RoleManager<LiteDbRole> GetRoleManager();
    }
}
