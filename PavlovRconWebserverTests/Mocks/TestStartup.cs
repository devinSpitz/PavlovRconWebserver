using Microsoft.Extensions.Configuration;
using PavlovRconWebserver;

namespace PavlovRconWebserverTests.UnitTests
{
    public class TestStartup : Startup
    {
        public TestStartup(IConfiguration env) : base(env)
        {
        }
    }
}