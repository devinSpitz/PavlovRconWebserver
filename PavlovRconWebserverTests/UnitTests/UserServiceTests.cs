using System.Linq;
using FluentAssertions;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Moq.AutoMock;
using PavlovRconWebserver.Services;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class UserServiceTests
    {
        private readonly IServicesBuilder services;
        private readonly AutoMocker _mocker;
        private readonly UserService _userService;
        private readonly RoleManager<LiteDbRole> _roleManager;
        private readonly UserManager<LiteDbUser> _userManager;
        public UserServiceTests() {
            services = new ServicesBuilder();
            _mocker = new AutoMocker();
            services.Build(_mocker);


            _roleManager = services.GetRoleManager();
            _mocker.Use( _roleManager );
            
            _userManager = services.GetUserManager();
            _mocker.Use( _userManager );
            
            _roleManager.CreateAsync(new LiteDbRole() {Name = "Admin"}).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new LiteDbRole() {Name = "Mod"}).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new LiteDbRole() {Name = "Captain"}).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new LiteDbRole() {Name = "User"}).GetAwaiter().GetResult();

            _userService = _mocker.CreateInstance<UserService>();
        }
        
        public static LiteDbUser SetUpUser(UserManager<LiteDbUser> manager, string name = "Test",string email = "test@test.com")
        {
            var user = new LiteDbUser()
            {
                UserName = name,
                Email = email,
            };

            manager.CreateAsync(user).GetAwaiter().GetResult();

            return user;
        }
        
        [Fact]
        public void FindAllInRole()
        {
            // arrange
            var user = SetUpUser(_userManager);
            var role = _roleManager.Roles.FirstOrDefault();
            _userManager.AddToRoleAsync(user, role.Name).GetAwaiter().GetResult();
            // act
            var usersResult = _userService.FindAllInRole(role.Name).GetAwaiter().GetResult().ToList();

            // assert
            usersResult.Should().NotBeNullOrEmpty();
            usersResult.Should().HaveCount(1);
        }
        
        
        [Fact]
        public void IsUserNotInRole()
        {
            // arrange
            var user = SetUpUser(_userManager);
            var role = _roleManager.Roles.FirstOrDefault();
            _userManager.AddToRoleAsync(user, role.Name).GetAwaiter().GetResult();
            // act
            
            var usersResult = !_userService.IsUserInRole(role.Name,user).GetAwaiter().GetResult();

            // assert
            usersResult.Should().BeFalse();
        }
        
        
        [Fact]
        public void FindAll()
        {
            // arrange
            SetUpUser(_userManager);
            // act
            var usersResult = _userService.FindAll().GetAwaiter().GetResult();

            // assert
            usersResult.Should().NotBeNullOrEmpty();
            usersResult.Should().HaveCount(1);
        }

        [Fact]
        public void Delete()
        {
            // arrange
            var user = SetUpUser(_userManager);
            var userResultBefor = _userManager.FindByIdAsync(user.Id.ToString()).GetAwaiter().GetResult();
            userResultBefor.Should().NotBeNull();
            // act
            var userResult = _userService.Delete(user.Id.ToString()).GetAwaiter().GetResult();
            var userResultAfter = _userManager.FindByIdAsync(user.Id.ToString()).GetAwaiter().GetResult();

            // assert
            userResult.Should().BeTrue();
            userResultAfter.Should().BeNull();
        }
        
        

    }
}