using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Libraries.APM
{
    public class Apm
    {
        private readonly ISystemEnvironment _operatingSystemEnvironment;
        private readonly ILogger _logger;

        public Apm(ILogger<Apm> logger)
        {
            // Inherit the logger.
            _logger = logger;

            // Check if we have a supported operating system.
            if (AppConfig.isWindows)
            {
                _operatingSystemEnvironment = new WindowsEnvironment();
            }
            else if (AppConfig.isLinux)
            {
                _operatingSystemEnvironment = new LinuxEnvironment();
            }
            else if (AppConfig.isMac)
            {
                _operatingSystemEnvironment = new MacEnvironment();
            }
            else
            {
                logger.LogError("APM was instantiated on unsupported opearting system.");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get all details from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetAllDetails()
        {
            return _operatingSystemEnvironment.GetAllDetails();
        }

        /// <summary>
        /// Get information about the processor from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetCpuDetails()
        {
            return _operatingSystemEnvironment.GetCpuDetails();
        }

        /// <summary>
        /// Get memory details from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            return _operatingSystemEnvironment.GetMemoryDetails();
        }

        /// <summary>
        /// Shorthand function to get the environment details of the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetEnvironmentDetails()
        {
            return _operatingSystemEnvironment.GetEnvironmentDetails();
        }

        /// <summary>
        /// Get the instance of the operating system environment handler.
        ///
        /// This is extendable: Apm.SystemEnvironment().GetCpuDetails();
        /// </summary>
        /// <returns></returns>
        public ISystemEnvironment SystemEnvironment() => _operatingSystemEnvironment;
    }
}