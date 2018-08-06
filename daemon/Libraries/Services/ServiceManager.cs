using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Core.Statistics;

namespace Spectero.daemon.Libraries.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IStatistician _statistician;
        private readonly IMemoryCache _cache;
        private readonly IProcessRunner _processRunner;
        
        private readonly IEnumerable<IPAddress> _localAddresses = Utility.GetLocalIPs();
        private readonly ConcurrentDictionary<Type, IService> _services = new ConcurrentDictionary<Type, IService>();

        private bool initiated = false;

        public ServiceManager(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IStatistician statistician, IServiceConfigManager serviceConfigManager,
            IMemoryCache cache, IProcessRunner processRunner)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _statistician = statistician;
            _serviceConfigManager = serviceConfigManager;
            _cache = cache;
            _processRunner = processRunner;

            _localNetworks = Utility.GetLocalRanges(_logger);
        }


        public string Process(string name, string action, out String error)
        {
            InitiateServices();
            error = null;
            Type type = Utility.GetServiceType(name);
            var service = GetService(type);
            string message = null;

            if (service == null)
            {
                var errorMessage =
                    string.Format("NAP: Resolved service was null when processing name-> {0}, action-> {1}",
                        name, action);
                error = errorMessage;
                _logger.LogError(errorMessage);
                return Messages.ACTION_FAILED;
            }

            try
            {
                switch (action.ToLower())
                {
                    case "start":
                        service.Start();
                        message = Messages.SERVICE_STARTED;
                        break;
                    case "stop":
                        service.Stop();
                        message = Messages.SERVICE_STOPPED;
                        break;
                    case "restart":
                        service.ReStart();
                        message = Messages.SERVICE_RESTARTED;
                        break;
                }
            }
            catch (Exception e)
            {
                var errorBuilder = new StringBuilder(
                    string.Format("Processing {0} failed on {1}.{2}", action, name, Environment.NewLine)
                );
                bool alreadyLogged = false;

                if (type == typeof(HTTPProxy.HTTPProxy))
                {
                    // We possibly have some custom data we can include, thus special handling.
                    if (e.Data.Count > 0)
                    {
                        if (e.InnerException is SocketException)
                            errorBuilder.AppendLine(
                                "listen() failed at one or more of the specified endpoints. Details: ");

                        var customErrorMessage = new StringBuilder("");

                        foreach (var customData in e.Data)
                        {
                            // Not sure I understand why a null entry is even possible, but SolarLint says so, it must be true (!)
                            if (customData == null) continue;

                            var localData = (DictionaryEntry) customData;
                            customErrorMessage.Append(localData.Key + " " + localData.Value + " ");
                        }

                        errorBuilder.AppendLine(customErrorMessage.ToString());
                    }

                    error = errorBuilder.ToString();
                    _logger.LogError(e, error);
                    alreadyLogged = true;
                   
                }

                message = Messages.ACTION_FAILED;

                if (!alreadyLogged)
                {
                    _logger.LogError(e, message);
                    error = e.Message;
                }
                
                // To ensure consistency, FORCE a stop
                _logger.LogWarning($"Forcing service ({name}) to stop as startup failed unexpectedly.");
                service.Stop();
            }

            return message;
        }


        public IService GetService(Type type)
        {
            InitiateServices();
            if (_services.ContainsKey(type))
                return _services[type];
            else
            {
                _logger.LogError("SCORG: Requested service of type " + type +
                                 " was not found. Perhaps initialization failed?");
                return null;
            }
        }

        private void InitiateServices()
        {
            if (initiated)
                return;

            var type = typeof(IService);
            var implementers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p))
                .Where(p => p != type) // Skip `IService` itself, it cannot be activated
                .ToArray();


            _logger.LogDebug("IS: Found " + implementers.Length + " services to activate.");

            foreach (var serviceType in implementers)
            {
                _logger.LogDebug("IS: Processing activation request for " + serviceType);
                var service = (IService) Activator.CreateInstance(serviceType, _appConfig, _logger, _db, _authenticator,
                    _localNetworks, _localAddresses, _statistician, _cache, _processRunner);
                var config = _serviceConfigManager.Generate(serviceType);
                service.SetConfig(config);
                _logger.LogDebug("IS: Activation succeeded for " + serviceType);
                _services.TryAdd(serviceType, service);
            }

            _logger.LogDebug("IS: Successfully initialized " + _services.Count + " service(s).");
            initiated = true;
        }

        public void StopServices()
        {
            // Nothing to kill bruh.
            if (! initiated)
                return;

            // Let's stop it all.
            foreach (var service in GetServices())
            {
                _logger.LogDebug($"Processing stop request for {service.Key}");
                service.Value.Stop();
            }
        }

        public ConcurrentDictionary<Type, IService> GetServices()
        {
            InitiateServices();
            return _services;
        }
    }
}