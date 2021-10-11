using System.Net.Http;
using System.Net.Http.Headers;
using PavlovRconWebserverTests.Mocks;
using Xunit;

namespace PavlovRconWebserverTests.UnitTests
{
    public class HomeControllerTest
    {
        /// <summary>
        ///     WIP todo full integration tests
        /// </summary>

        // [Fact]
        public void IndexAdmin()
        {
            // arrange
            var server = IntegrationTest.RunTestHost();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            //
            // var user = IntegrationTest.MockUsers("Admin").GetAwaiter().GetResult();
            // Thread.CurrentPrincipal = user.Principal;
            // var controller = mocker.CreateInstance<HomeController>();
            var request = new HttpRequestMessage(HttpMethod.Post, "/Home");
            request.Headers.Authorization = new AuthenticationHeaderValue("Test");
            // Act
            var response = client.SendAsync(request).GetAwaiter().GetResult();

            // Assert
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("Pavlov Rcon Webserver", responseString);
            Assert.Contains("User Management", responseString);
            Assert.Contains("Servers", responseString);
            Assert.Contains("Matchmaking", responseString);
            //DefaultDB(true);
        }

        // [Fact]
        public void IndexMod()
        {
            // arrange
            var server = IntegrationTest.RunTestHost();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Mod");
            // Act
            var response = client.GetAsync("/Home").GetAwaiter().GetResult();

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("Pavlov Rcon Webserver", responseString);
            Assert.Contains("User Management", responseString);
            Assert.Contains("Servers", responseString);
            Assert.Contains("Matchmaking", responseString);
            //DefaultDB(true);
        }

        // [Fact]
        public void IndexCap()
        {
            // arrange
            var server = IntegrationTest.RunTestHost();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Mod");
            // Act
            var response = client.GetAsync("/Home").GetAwaiter().GetResult();

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("Pavlov Rcon Webserver", responseString);
            Assert.Contains("User Management", responseString);
            Assert.Contains("Servers", responseString);
            Assert.Contains("Matchmaking", responseString);
            //DefaultDB(true);
        }

        // [Fact]
        public void IndexNoAuth()
        {
            // arrange
            var server = IntegrationTest.RunTestHost();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = client.GetAsync("/Home").GetAwaiter().GetResult();

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("Pavlov Rcon Webserver", responseString);
            //DefaultDB(true);
        }
    }
}