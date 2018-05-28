using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Statistics;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPN : IService
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly IEnumerable<IPAddress> _localAddresses;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IStatistician _statistician;
        private readonly IMemoryCache _cache;
        private IEnumerable<OpenVPNConfig> _vpnConfig;


        private readonly ServiceState State = ServiceState.Halted;
        private readonly List<string> _configsOnDisk;



        public OpenVPN()
        {
        }

        public OpenVPN(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IEnumerable<IPAddress> localAddresses,
            IStatistician statistician, IMemoryCache cache)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _statistician = statistician;
            _cache = cache;
            _localAddresses = localAddresses;

            // This is tracked so we can clean it up when stopping (assuming managed stop).
            _configsOnDisk = new List<string>();
        }

        private void Initialize(IEnumerable<IServiceConfig> serviceConfigs)
        {
            if (serviceConfigs != null)
                _vpnConfig = serviceConfigs as List<OpenVPNConfig>;

            if (_vpnConfig == null || !_vpnConfig.Any())
                throw new InvalidOperationException("OpenVPN init: config list was null.");

            // Now, let's render the configurations into proper OpenVPN config files.
            var renderedConfigs = _vpnConfig.Select(x => x.GetStringConfig().Result);

            foreach (var config in renderedConfigs)
            {
                var onDiskName = Path.GetTempPath() + Guid.NewGuid() + ".ovpn";
                using (var writer = new StreamWriter(onDiskName))
                {
                    writer.Write(config);
                }
                _configsOnDisk.Add(onDiskName);

                //At this stage, we have the configs ready and on disk. Let us simply bootstrap the processes.
                StartDaemon(onDiskName);
            }

            // TODO: Invoke OpenVPN once per config on disk and track the process handle somewhere.
            // TODO: We also need to hook into netlink+netfilter (or its OS specific counterparts) to enable MASQUERADE/SNAT for our NATed IPs.


        }

        private string determineBinaryPath()
        {
            string binaryName;

            if (AppConfig.isUnix)
            {
                // We assume the binary to simply be called "openvpn" and in the system's global $PATH for seamless execution.
                binaryName = "openvpn";
            }
            else if (AppConfig.isWindows)
                binaryName = "openvpn"; // TODO: Fix this, we'll likely need the full path in Windows due to how we intend to ship the openvpn binary itself.
            else
                throw new PlatformNotSupportedException("OpenVPN: This daemon does not know how to initialize OpenVPN on this platform.");

            return binaryName;
        }

        private void StartDaemon(string configPath)
        {

            var path = determineBinaryPath();

            // TODO: Now, we call this with MedallionShell and store the Command descriptor.


        }

        public void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
           
            Initialize(serviceConfig);

        }

        public void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            _vpnConfig = serviceConfig as List<OpenVPNConfig>;
        }

        public void Reload(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            throw new NotSupportedException();
        }

        public void Stop()
        {
            // Let's get rid of the temporary config file(s) and clear the tracking object.
            foreach (var fileOnDisk in _configsOnDisk)
            {
                File.Delete(fileOnDisk);
            }
            _configsOnDisk.Clear();
        }

        public ServiceState GetState()
        {
            return State;
        }

        public void LogState(string caller)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<IServiceConfig> GetConfig()
        {
            return _vpnConfig;
        }

        public void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            _vpnConfig = config as List<OpenVPNConfig>;
        }
    }
}