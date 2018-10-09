using System;
using System.Configuration;
using CPUuseges.Helper;
using Topshelf;

namespace CPUuseges
{
    public class Core
    {
        private static void Main(string[] args)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("Start");

            var processCpuCounter = new ProcessCpuCounter();
            if (!long.TryParse(ConfigurationManager.AppSettings["intervalMilliSecond"], out var intervalMilliSecond))
            {
                intervalMilliSecond = 20 * 1000;
            }
            var applicationName = ConfigurationManager.AppSettings["Client"];

            try
            {
                var rc = HostFactory.Run(x =>
                {
                    x.Service<MonitorService>(s =>
                    {
                        s.ConstructUsing(name => new MonitorService(processCpuCounter, applicationName, intervalMilliSecond));
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });
                    x.RunAsLocalSystem();
                    x.SetDescription("Prototy application cpu monitor");
                    x.SetDisplayName("Cpu monitor");
                    x.SetDisplayName("CpuMonitorUsages");
                });
                var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
                Environment.ExitCode = exitCode;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in start");
            }

            logger.Info("Finish");
        }
    }
}
