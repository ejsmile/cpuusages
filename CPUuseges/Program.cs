using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace WMISample
{
    public class MyWMIQuery
    {
        static void Main(string[] args)
        {
            var dist = new Dictionary<ProcessInfo, int>();
            while (!Console.KeyAvailable)
            {
                var processes = Process.GetProcessesByName("Client");

                var list = new List<InfoCounter>();
                var delta = new Dictionary<ProcessInfo, float>();

                foreach (var p in processes)
                {
                    var item = ProcessCpuCounter.GetPerfCounterForProcessId(p.Id);
                    try
                    {
                        item.Counter.NextValue();
                    }
                    catch
                    {
                        continue;
                    }
                    list.Add(item);
                    delta[item.Description] = 0;
                }

                Console.WriteLine($"{DateTime.Now}  Client instans run {delta.Keys.Count} ");

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

                Console.WriteLine($"{DateTime.Now} Cpu over 15 % max duration {(dist.Any() ? dist.Values.Max() : 0) * 10} in sec ");

                foreach (var candidate in dist.Where(d => d.Value > 6 * 5))
                {
                    Console.WriteLine($"Warrings {DateTime.Now} {candidate.Key.Pid} {candidate.Key.Name} {candidate.Value * 10} sec -  Cpu over 15% time ");
                }
            }
        }
    }



    public struct ProcessInfo
    {
        public string Name { get; set; }
        public int Pid { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ProcessInfo other)
            {
                return Pid.Equals(other.Pid);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Pid.GetHashCode();
        }

        public static bool operator ==(ProcessInfo left, ProcessInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProcessInfo left, ProcessInfo right)
        {
            return !(left == right);
        }
    }

    public class InfoCounter
    {
        public PerformanceCounter Counter { get; set; }
        public ProcessInfo Description { set; get; }
    }
    public class ProcessCpuCounter
    {
        public static InfoCounter GetPerfCounterForProcessId(int processId, string processCounterName = "% Processor Time")
        {
            var instance = GetInstanceNameForProcessId(processId);
            if (string.IsNullOrEmpty(instance.Item1))
                return null;

            return new InfoCounter()
            {
                Counter = new PerformanceCounter("Process", processCounterName, instance.Item1),
                Description = instance.Item2
            };
        }

        public static Tuple<string, ProcessInfo> GetInstanceNameForProcessId(int processId)
        {
            var process = Process.GetProcessById(processId);
            string processName = Path.GetFileNameWithoutExtension(process.ProcessName);

            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
            string[] instances = cat.GetInstanceNames()
                .Where(inst => inst.StartsWith(processName))
                .ToArray();

            foreach (string instance in instances)
            {
                using (PerformanceCounter cnt = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == processId)
                    {
                        return new Tuple<string, ProcessInfo>(instance, new ProcessInfo() { Name = GetProcessOwner(processId), Pid = processId });
                    }
                }
            }
            return null;
        }

        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            using (var searcher = new ManagementObjectSearcher(query))
            {
                using (var processList = searcher.Get())
                {
                    foreach (ManagementObject obj in processList)
                    {
                        string[] argList = new string[] { string.Empty, string.Empty };
                        int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                        if (returnVal == 0)
                        {
                            // return DOMAIN\user
                            return argList[1] + "\\" + argList[0];
                        }
                    }

                    return "NO OWNER";
                }
            }
        }
    }
}
