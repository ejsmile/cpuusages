using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace CPUuseges
{
    public class ProcessCpuCounter : IProcessCpuCounter
    {
        public IEnumerable<int> GetProcessesIdByName(string name)
        { 
            return Process.GetProcessesByName(name).Select(p => p.Id);
        }

        public InfoCounter GetPerfCounterForProcessId(int processId, string processCounterName = "% Processor Time")
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

        public Tuple<string, ProcessInfo> GetInstanceNameForProcessId(int processId)
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

        public string GetProcessOwner(int processId)
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
