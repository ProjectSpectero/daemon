/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using Microsoft.AspNetCore.Hosting;
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
                    _logger.LogInformation("Autostart: Attempting to init OpenVPN on non-Linux system, implementation is incomplete and may be broken.");

                var result = _manager.Process(service, "start", out var error);

                if (result == Messages.ACTION_FAILED)
                {
                    _logger.LogCritical($"Autostart: Processing failed for {service} due to {error}");

                    // Quit if the config dictates that we must, this allows failure tracking by NSM/SystemD/whatever else
                    if (_appConfig.HaltStartupIfServiceInitFails)
                    {
                        _logger.LogCritical($"Autostart: failed to start {service}, config dictates we must gracefully exit.");
                        _applicationLifetime.StopApplication();
                    }
                        
                }

                else
                    _logger.LogInformation(string.Format("Autostart: Processed autostartup for {0}.", service));
            }
        }
    }
}