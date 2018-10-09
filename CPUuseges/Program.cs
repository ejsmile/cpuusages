using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CPUuseges
{
    public class Core
    {
        static void Main(string[] args)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("Start");

            var processCpuCounter = new ProcessCpuCounter();

            var dist = new Dictionary<ProcessInfo, int>();
            while (!Console.KeyAvailable)
            {
                var processeIds = processCpuCounter.GetProcessesIdByName("Client");

                var list = new List<InfoCounter>();
                var delta = new Dictionary<ProcessInfo, float>();

                foreach (var id in processeIds)
                {
                    var item = processCpuCounter.GetPerfCounterForProcessId(id);
                    try
                    {
                        item.Counter.NextValue();
                    }
                    catch
                    {
                        //application close
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
                        if (dist.ContainsKey(key))
                            dist[key]++;
                        else
                            dist[key] = 1;
                    }
                    else
                    {
                        if (dist.ContainsKey(key))
                            dist.Remove(key);
                    }
                }
                logger.Info($"Cpu over 15 % max duration {(dist.Any() ? dist.Values.Max() : 0) * 10} in sec");

                foreach (var candidate in dist.Where(d => d.Value > 6 * 5))
                {
                    logger.Warn($"{candidate.Key.Pid} {candidate.Key.Name} {candidate.Value * 10} sec -  Cpu over 15% time ");
                }
            }
            logger.Info("Finish");
        }
    }

    
    
    
}
