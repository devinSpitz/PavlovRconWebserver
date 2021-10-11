using Hangfire.Dashboard;

namespace PavlovRconWebserver.Extensions
{
    public class HangfireAuthorizeFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpcontext = context.GetHttpContext();
            return httpcontext.User.IsInRole("Admin");
        }
    }
}