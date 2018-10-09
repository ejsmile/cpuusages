using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CPUuseges.Helper;

namespace CPUuseges
{
    public class MonitorService : IDisposable
    {
        private bool disposed = false;
        private readonly Dictionary<ProcessInfo, int> state;
        private readonly IProcessCpuCounter cpuCounter;
        private readonly NLog.ILogger logger;
        private readonly Timer timer;
        private readonly long interval;
        private readonly string applicationName;

        public MonitorService(IProcessCpuCounter cpuCounter, string applicationName, long interval)
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            this.cpuCounter = cpuCounter;
            this.interval = interval;
            this.applicationName = applicationName;
            timer = new Timer((o) => Worker(),
                null,
                Timeout.Infinite,
                Timeout.Infinite
                );
            state = new Dictionary<ProcessInfo, int>();
        }

        private void Worker()
        {
            var processeIds = cpuCounter.GetProcessesIdByName(applicationName);

            var list = new List<InfoCounter>();
            var delta = new Dictionary<ProcessInfo, float>();

            foreach (var id in processeIds)
            {
                var item = cpuCounter.GetPerfCounterForProcessId(id);
                try
                {
                    item.Counter.NextValue();
                }
                catch
                {
                    //application close when monitor
                    continue;
                }
                list.Add(item);
                delta[item.Description] = 0;
            }

            logger.Info($"Client instans run {delta.Keys.Count} ");

            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                foreach (var counter in list)
                {
                    if (counter.Counter == null) continue;
                    try
                    {
                        delta[counter.Description] += (counter.Counter.NextValue() / (float)Environment.ProcessorCount);
                    }
                    catch
                    {
                        counter.Counter = null;
                        delta[counter.Description] = 0;
                        continue;
                    }
                }
            }

            foreach (var key in delta.Keys)
            {
                if (delta[key] / 10 > 15)
                {
                    if (state.ContainsKey(key))
                        state[key]++;
                    else
                        state[key] = 1;
                }
                else
                {
                    if (state.ContainsKey(key))
                        state.Remove(key);
                }
            }

            logger.Info($"Cpu over 15 % max duration {(state.Any() ? state.Values.Max() : 0) * 10} in sec");

            foreach (var candidate in state.Where(d => d.Value > 6 * 5))
            {
                logger.Warn($"{candidate.Key.Pid} {candidate.Key.Name} {candidate.Value * 10} sec -  Cpu over 15% time ");
            }
        }

        public void Start()
        {
            timer.Change(0, interval);
        }

        public void Stop()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            state.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
