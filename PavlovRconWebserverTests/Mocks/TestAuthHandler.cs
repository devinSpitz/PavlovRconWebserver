using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PavlovRconWebserverTests.Mocks
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

             
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test user"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            
            var result = AuthenticateResult.Success(ticket);
            
            return Task.FromResult(result);
            //Request.Headers.TryGetValue("Authorization", out var auth);
            //
            // AuthenticationTicket ticket = null;
            // if (auth.FirstOrDefault() == "Admin")
            // {
            //     var claims = new[]
            //     {
            //         new Claim(ClaimTypes.Name, "test"),
            //         new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            //         new Claim(ClaimTypes.Role, "Admin"),
            //
            //     };
            //     var identity = new ClaimsIdentity(claims, "Admin");
            //     var principal = new ClaimsPrincipal(identity);
            //     ticket = new AuthenticationTicket(principal, "Test");
            // }
            // else if (auth.FirstOrDefault() == "Mod")
            // {
            //     var claims = new[]
            //     {
            //         new Claim(ClaimTypes.Name, "test"),
            //         new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            //         new Claim(ClaimTypes.Role, "Mod"),
            //     };
            //     var identity = new ClaimsIdentity(claims, "Mod");
            //     var principal = new ClaimsPrincipal(identity);
            //     ticket = new AuthenticationTicket(principal, "Test");
            // }
            // else if (auth.FirstOrDefault() == "Captain")
            // {
            //     var claims = new[]
            //     {
            //         new Claim(ClaimTypes.Name, "test"),
            //         new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            //         new Claim(ClaimTypes.Role, "Captain"),
            //
            //     };
            //     var identity = new ClaimsIdentity(claims, "Captain");
            //     var principal = new ClaimsPrincipal(identity);
            //     ticket = new AuthenticationTicket(principal, "Test");
            //
            // }
            // else if (auth.FirstOrDefault() == "User")
            // {
            //     var claims = new[]
            //     {
            //         new Claim(ClaimTypes.Name, "test"),
            //         new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            //         new Claim(ClaimTypes.Role, "Captain"),
            //     };
            //     var identity = new ClaimsIdentity(claims, "User");
            //     var principal = new ClaimsPrincipal(identity);
            //     ticket = new AuthenticationTicket(principal, "Test");
            //
            // }
            //
            // var result = AuthenticateResult.Success(ticket);
            //
            // return Task.FromResult(result);
        }
    }
}