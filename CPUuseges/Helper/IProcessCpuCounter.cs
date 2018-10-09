using System.Collections.Generic;

namespace CPUuseges.Helper
{
    public interface IProcessCpuCounter
    {
        IEnumerable<int> GetProcessesIdByName(string name);

        InfoCounter GetPerfCounterForProcessId(int processId);

        InfoCounter GetPerfCounterForProcessId(int processId, string processCounterName);
    }
}
