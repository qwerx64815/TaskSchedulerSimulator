using Hangfire;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System.Diagnostics;
using System.Reflection;

namespace TaskSchedulerSimulator.Helper
{
    class SchedulerHelper
    {
        public static readonly Logger logger = LogManager.Setup()
                                                  .LoadConfigurationFromAppSettings()
                                                  .GetCurrentClassLogger();

        private RunAllTaskSchedule _RunAllTaskSchedule { get; set; }

        public SchedulerHelper()
        {
            _RunAllTaskSchedule = new RunAllTaskSchedule();
        }

        public void InitializeSchedule()
        {
            _RunAllTaskSchedule.Initialization();
        }

        private class RunAllTaskSchedule
        {
            private List<Task> TaskList = [];

            public RunAllTaskSchedule()
            {
                GetTaskList();
            }

            public class Task
            {
                public string FilePath { get; set; } = string.Empty;
                public string TriggerTime { get; set; } = string.Empty;
            }
            public void GetTaskList()
            {
                try
                {
                    var TaskListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TaskList.json");
                    if (!File.Exists(TaskListPath))
                    {
                        logger.Error($"TaskList file not found! Path: {TaskListPath}");
                    }
                    else
                    {
                        using StreamReader reader = new(TaskListPath);
                        var FileContent = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(FileContent))
                        {
                            TaskList = JsonConvert.DeserializeObject<List<Task>>(FileContent);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Occurs error when load task list.");
                }
            }

            public void Initialization()
            {
                try
                {
                    if (TaskList.Count != 0)
                    {
                        foreach (var Task in TaskList)
                        {
                            var FilePath = Task.FilePath;

                            var AbsolutePath = Path.Combine(AppContext.BaseDirectory, FilePath);
                            if (!File.Exists(AbsolutePath))
                            {
                                logger.Error($"Task file {AbsolutePath} not found!");
                                continue;
                            }

                            var Filename = Path.GetFileName(FilePath);
                            var TriggerTime = Task.TriggerTime.Trim();
                            logger.Info($"Set task job, TriggerTime: {TriggerTime}, Filename: {Filename}, FilePath: {FilePath}");

                            if (!string.IsNullOrWhiteSpace(TriggerTime))
                            {
                                var JobID = Filename;
                                RecurringJob.RemoveIfExists(JobID);

                                var TimeZoneOptions = new RecurringJobOptions
                                {
                                    TimeZone = TimeZoneInfo.Local
                                };

                                RecurringJob.AddOrUpdate(JobID, () => RunTask(Task), TriggerTime, TimeZoneOptions);
                            }
                            else
                            {
                                logger.Error("Task trigger time not exists, task job not created.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            public void RunTask(Task TaskInfo)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = TaskInfo.FilePath,
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            RedirectStandardOutput = false, 
                            RedirectStandardError = false,
                        },
                    };

                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
        }
    }
}
