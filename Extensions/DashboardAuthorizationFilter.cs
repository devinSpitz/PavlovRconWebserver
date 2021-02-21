using Hangfire.Annotations;
using Hangfire.Dashboard;
using LiteDB.Identity.Database;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PavlovRconWebserver.Services;

namespace PavlovRconWebserver.Extensions
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            
            if (context.GetHttpContext().User.Identity.IsAuthenticated &&
                context.GetHttpContext().User.IsInRole("Admin"))
                return true;
            return  false;
        }

    }
}