using Hangfire.Dashboard;

namespace TaskSchedulerSimulator.Utils
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// 允許任何人訪問 Hangfire 後台。
        /// </summary>
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
