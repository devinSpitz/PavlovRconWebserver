using LiteDB.Identity.Async.Database;
using LiteDB.Identity.Async.Extensions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq.AutoMock;

namespace PavlovRconWebserverTests.Mocks
{
    internal class ServicesBuilder : IServicesBuilder
    {
        private readonly IServiceCollection services;
        private ServiceProvider provider;

        public ServicesBuilder()
        {
            services = new ServiceCollection();
        }

        public void Build(AutoMocker mocker)
        {
            services.AddHttpContextAccessor();
            services.AddLogging();
            services.AddLiteDbIdentityAsync("Filename=:memory:;");
            provider = services.BuildServiceProvider();
            var db = provider.GetService<ILiteDbIdentityAsyncContext>();
            mocker.Use(db);
            var roleManager = provider.GetService<RoleManager<LiteDbRole>>();
            mocker.Use(roleManager);
            var userManager = provider.GetService<UserManager<LiteDbUser>>();
            mocker.Use(userManager);
        }

        public RoleManager<LiteDbRole> GetRoleManager()
        {
            return provider.GetService<RoleManager<LiteDbRole>>();
        }

        public UserManager<LiteDbUser> GetUserManager()
        {
            return provider.GetService<UserManager<LiteDbUser>>();
        }
    }
}