using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

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
                var service = entry.Value;
                service.Start();
                _logger.LogInformation("S: Processed autostartup for " + entry.Key);
            }

        }
    }
}