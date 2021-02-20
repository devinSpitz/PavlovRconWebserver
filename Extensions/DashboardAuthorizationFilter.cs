using Hangfire.Annotations;
using Hangfire.Dashboard;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var userIdentity = context.GetHttpContext().User.Identity;
            if (userIdentity != null && userIdentity.IsAuthenticated && context.GetHttpContext().User.IsInRole("Admin"))
                return true;
            return  false;
        }
    }
}