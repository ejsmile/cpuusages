using System.Collections.Generic;

namespace CPUuseges
{
    public interface IProcessCpuCounter
    {
        IEnumerable<int> GetProcessesIdByName(string name);
        InfoCounter GetPerfCounterForProcessId(int processId, string processCounterName);
    }
}
