using Hangfire;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System.Diagnostics;

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
                public bool PythonScriptMode { get; set; } = false;
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
                    var ProcessInfo = new ProcessStartInfo();

                    if (TaskInfo.PythonScriptMode)
                    {
                        ProcessInfo.FileName = "python";
                        ProcessInfo.Arguments = $"\"{TaskInfo.FilePath}\"";
                        ProcessInfo.CreateNoWindow = true;
                        ProcessInfo.UseShellExecute = false;
                        ProcessInfo.RedirectStandardOutput = true;
                        ProcessInfo.RedirectStandardError = true;
                    }
                    else
                    {
                        ProcessInfo.FileName = TaskInfo.FilePath;
                        ProcessInfo.CreateNoWindow = true;
                        ProcessInfo.UseShellExecute = false;
                        ProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ProcessInfo.RedirectStandardOutput = false;
                        ProcessInfo.RedirectStandardError = false;
                    }

                    using var _Process = Process.Start(ProcessInfo);

                    if (_Process != null)
                    {
                        var Output = _Process.StandardOutput.ReadToEnd();
                        var Error = _Process.StandardError.ReadToEnd();

                        _Process.WaitForExit();

                        logger.Info($"Output: {Output}");

                        if (!string.IsNullOrEmpty(Error))
                            logger.Info($"Error: {Error}");
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
        }
    }
}
