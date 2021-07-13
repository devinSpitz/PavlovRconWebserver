using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace PavlovRconWebserver.Extensions
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            if (context.GetHttpContext().User.Identity.IsAuthenticated &&
                context.GetHttpContext().User.IsInRole("Admin"))
                return true;
            return false;
        }
    }
}