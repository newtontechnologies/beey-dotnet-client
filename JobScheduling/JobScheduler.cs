using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    class JobScheduler
    {
        private static readonly TimeSpan noRepeatInterval = TimeSpan.FromMilliseconds(-1);
        private int idCounter = 0;
        private ConcurrentDictionary<int, JobData> activeJobs = new ConcurrentDictionary<int, JobData>();

        // TODO: check if Wait() is OK
        public int ScheduleJob(Func<Task> job, Action<Exception> onException, DateTime date)
            => ScheduleJob(() => job().Wait(), onException, date, null);
        public int ScheduleJob(Func<Task> job, Action<Exception> onException, DateTime date, TimeSpan? repeatInterval)
            => ScheduleJob(() => job().Wait(), onException, date, repeatInterval);
        public int ScheduleJob(Action job, Action<Exception> onException, DateTime date)
            => ScheduleJob(job, onException, date, null);
        public int ScheduleJob(Action job, Action<Exception> onException, DateTime date, TimeSpan? repeatInterval)
        {
            int timerId = Interlocked.Increment(ref idCounter);
            var goTime = date - DateTime.Now;
            if (repeatInterval.HasValue && repeatInterval <= TimeSpan.Zero)
                repeatInterval = null;
            var callback = CreateCallback(job, onException, repeatInterval.HasValue);
            // Set start in 1s to ensure that when the timer runs, it is already in activeJobs.
            if (goTime < TimeSpan.FromSeconds(1))
                goTime = TimeSpan.FromSeconds(1);
            var timer = new Timer(new TimerCallback(callback), timerId, goTime, repeatInterval ?? noRepeatInterval);
            activeJobs.TryAdd(timerId, new JobData(timer, date, repeatInterval ?? (TimeSpan?)null));
            return timerId;
        }

        public List<KeyValuePair<int,JobData>> ListJobs()
            => activeJobs.ToList();
        public bool CancelJob(int jobId)
        {
            var job = activeJobs.GetValueOrDefault(jobId);
            job?.Timer.Dispose();
            activeJobs.TryRemove(jobId, out _);
            return job != null;
        }

        private Action<object> CreateCallback(Action job, Action<Exception> onException, bool isRepeating)
        {
            return (state) =>
            {
                try
                {
                    job();
                }
                catch (Exception ex)
                {
                    onException(ex);
                }
                finally
                {
                    if (!isRepeating && state != null)
                    {
                        if (!CancelJob((int)state))
                        {
                            // TODO implement logging
                            Console.WriteLine("Error when cleaning up. Job data does not exist.");
                        }
                    }
                }
            };
        }

        public class JobData
        {
            public Timer Timer { get; set; }
            public DateTime DateToRun { get; set; }
            public TimeSpan? RepeatInterval { get; set; }

            public JobData(Timer timer, DateTime dateToRun) : this (timer, dateToRun, null)
            { }
            public JobData(Timer timer, DateTime dateToRun, TimeSpan? repeatInterval)
            {
                Timer = timer;
                DateToRun = dateToRun;
                RepeatInterval = repeatInterval;
            }
        }
    }
}
