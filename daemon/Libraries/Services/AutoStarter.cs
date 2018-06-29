using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;

namespace Spectero.daemon.Libraries.Services
{
    public class AutoStarter : IAutoStarter
    {
        private readonly IServiceManager _manager;
        private readonly ILogger<AutoStarter> _logger;
        private readonly AppConfig _appConfig;
        private readonly IApplicationLifetime _applicationLifetime;

        public AutoStarter(IServiceManager manager, ILogger<AutoStarter> logger,
            IOptionsMonitor<AppConfig> configMonitor, IApplicationLifetime applicationLifetime)
        {
            _manager = manager;
            _logger = logger;
            _appConfig = configMonitor.CurrentValue;
            _applicationLifetime = applicationLifetime;
        }

        public void Startup()
        {
            if (!_appConfig.AutoStartServices) return;

            foreach (var entry in _manager.GetServices())
            {
                var service = entry.Value.GetType().Name;

                if (service.Equals("OpenVPN") && !AppConfig.isLinux)
                {
                    // TODO: Enable OpenVPN in non-Linux platforms when readay.
                    _logger.LogInformation("Autostart: Skipping OpenVPN auto-start on non-Linux system, implementation is incomplete.");
                    continue;
                }


                var result = _manager.Process(service, "start", out var error);

                if (result == Messages.ACTION_FAILED)
                {
                    _logger.LogError(string.Format(
                        "Autostart: Processing failed for {0}\n" +
                        "(Reason: {1})", service, error
                    ));

                    // Quit if the config dictates that we must, this allows failure tracking by NSM/SystemD/whatever else
                    if (_appConfig.HaltStartupIfServiceInitFails)
                        _applicationLifetime.StopApplication();
                        
                }

                else
                    _logger.LogInformation(string.Format("Autostart: Processed autostartup for {0}.", service));
            }
        }
    }
}