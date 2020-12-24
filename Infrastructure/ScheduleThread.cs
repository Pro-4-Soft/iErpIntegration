using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Workers;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class ScheduleThread
    {
        private static ScheduleThread _instance;
        public static ScheduleThread Instance => _instance ??= new ScheduleThread();

        internal static Lazy<Process> Process => new Lazy<Process>(System.Diagnostics.Process.GetCurrentProcess);

        readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private bool _isRunning = true;
        private Thread _scheduleThread;
        private List<WorkerThread> _workerThreads = new List<WorkerThread>();

        private void RunSchedulling()
        {
            _workerThreads.ForEach(c => c.Stop());
            _workerThreads = App<Settings>.Instance.Schedules.Select(c => c.ThreadName).Distinct().Select(c => new WorkerThread(c)).ToList();

            foreach (var taskOnStartup in App<Settings>.Instance.Schedules
                .Where(c => c.Active)
                .Where(c => c.RunOnStartup))
                RunTask(taskOnStartup);

            while (_isRunning)
            {
                var now = DateTimeOffset.Now;
                var (sleepTime, tasksToExecute) = App<Settings>.Instance.NextTime(now);
                if (!tasksToExecute.Any())
                {
                    Report($"Nothing to execute").Wait();
                    break;
                }

                _resetEvent.WaitOne(sleepTime);
                if (!_isRunning)
                    break;

                foreach (var poll in tasksToExecute)
                    RunTask(poll);
            }

            Report($"Stopped").Wait();
        }

        public void RunTask(ScheduleSetting task)
        {
            if (task == null)
                return;
            _workerThreads.SingleOrDefault(c => c.Name == task.ThreadName)?.ScheduleExecute(task);
        }

        internal static async Task Report(string msg)
        {
            await Console.Out.WriteLineAsync(msg);
        }

        internal static async Task ReportErrorAsync(Exception ex)
        {
            await ReportErrorAsync(ex.ToString());
        }

        internal static async Task ReportErrorAsync(string msg)
        {
            await Console.Error.WriteLineAsync(msg);
        }

        public void Start()
        {
            Singleton<WebEventListener>.Instance.Start();

            _scheduleThread = new Thread(RunSchedulling);
            _scheduleThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _resetEvent.Set();
            _scheduleThread.Join();
            _workerThreads.ForEach(c => c.Stop());
            _workerThreads = new List<WorkerThread>();
        }
    }
}
