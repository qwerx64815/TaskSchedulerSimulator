using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using TaskSchedulerSimulator.Helper;
using TaskSchedulerSimulator.Utils;
using static System.Configuration.ConfigurationManager;

var logger = LogManager.Setup()
                       .LoadConfigurationFromAppSettings()
                       .GetCurrentClassLogger();

var pubService = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
logger.Info($"{pubService} {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
logger.Info("\n============== Initialize process ==============\n");

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseColouredConsoleLogProvider()
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage());
        services.AddHangfireServer();

        services.AddSingleton<SchedulerHelper>();
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
        webBuilder.UseUrls($"http://*:{AppSettings["ScheduleDashboardPort"]}");
    });

var app = builder.Build();

try
{
    var backgroundJobClient = app.Services.GetRequiredService<IBackgroundJobClient>();
    backgroundJobClient.Enqueue(() => Console.WriteLine(">>> Hangfire started!"));

    var scheduleHelper = app.Services.GetService<SchedulerHelper>();
    if (scheduleHelper != null) scheduleHelper.InitializeSchedule();

    app.Run();
}
catch (Exception e)
{
    logger.Error(e.ToString());
}
finally
{
    app.WaitForShutdown();
}


class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHangfireDashboard();
        });

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });
    }
}