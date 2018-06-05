using System;
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

        public AutoStarter(IServiceManager manager, ILogger<AutoStarter> logger,
            IOptionsMonitor<AppConfig> configMonitor)
        {
            _manager = manager;
            _logger = logger;
            _appConfig = configMonitor.CurrentValue;
        }

        public void Startup()
        {
            if (!_appConfig.AutoStartServices) return;

            foreach (var entry in _manager.GetServices())
            {
                var service = entry.Value.GetType().Name;

                var result = _manager.Process(service, "start", out var error);

                if (result == Messages.ACTION_FAILED)
                {
                    _logger.LogError(string.Format(
                        "Autostart: Processing failed for {0}\n" +
                        "(Reason: {1})", service, error
                    ));

                    // Quit if the config dictates that we must, this allows failure tracking by NSM/SystemD/whatever else
                    if (_appConfig.HaltStartupIfServiceInitFails)
                        Environment.Exit(-1);
                }

                else
                    _logger.LogInformation(string.Format("Autostart: Processed autostartup for {0}.", service));
            }
        }
    }
}