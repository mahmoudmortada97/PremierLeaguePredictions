using Hangfire.Dashboard;

namespace PremierLeaguePredictions.Filters
{
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        private readonly string _validKey;

        public HangfireDashboardAuthFilter(string validKey) => _validKey = validKey;

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.Request.Query.TryGetValue("key", out var key)
                && key == _validKey;
        }
    }
}