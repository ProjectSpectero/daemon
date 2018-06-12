using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;

namespace daemon_testcases
{
    [TestFixture]
    public class ProccessRunnerTest
    {
        [Test]
        public void Restart()
        {
            // Build a fake service
            IService service = new FakeService();


            ILogger<ProcessRunner> logger = new Mock<ILogger<ProcessRunner>>().Object;
            IOptionsMonitor<AppConfig> configMonitor = new Mock<OptionsMonitor<AppConfig>>().Object;

            // Get a process runner going.
            ProcessRunner processRunner = new ProcessRunner(configMonitor, logger);

            // Build the command options
            ProcessOptions processOptions = new ProcessOptions()
            {
                Executable =
                    AppConfig.isUnix
                        ? "top"
                        : "cmd", // top and cmd are both processes that will run continiously until closed.
                DisposeOnExit = true,
                Monitor = true,
            };

            // Run the example command.
            processRunner.Run(processOptions, service);

            // Sleep 1 seconds before initializing restart
            Thread.Sleep(10000);
            processRunner.RestartAllTrackedCommands();
        }
    }

    /// <summary>
    /// The sole purpose of this fake service, is to pass as a variable to the process runner.
    /// </summary>
    public class FakeService : IService
    {
        // I added this specifically, as I knwo it is state dependent.
        private readonly ServiceState _state = ServiceState.Running;

        public IEnumerable<IServiceConfig> GetConfig()
        {
            throw new System.NotImplementedException();
        }

        public ServiceState GetState()
        {
            throw new System.NotImplementedException();
        }

        public void LogState(string caller)
        {
            throw new System.NotImplementedException();
        }

        public void Reload(IEnumerable<IServiceConfig> serviceConfig)
        {
            throw new System.NotImplementedException();
        }

        public void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            throw new System.NotImplementedException();
        }

        public void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            throw new System.NotImplementedException();
        }

        public void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}