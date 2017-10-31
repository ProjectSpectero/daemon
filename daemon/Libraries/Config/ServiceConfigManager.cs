using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Config
{
    public class ServiceConfigManager : IServiceConfigManager
    {
        private readonly AppConfig _appConfig;
        private readonly IDbConnection _db;
        private readonly ILogger<ServiceConfigManager> _logger;
        private readonly ICryptoService _cryptoService;

        public ServiceConfigManager(IOptionsMonitor<AppConfig> config, ILogger<ServiceConfigManager> logger,
            IDbConnection db, ICryptoService cryptoService)
        {
            _appConfig = config.CurrentValue;
            _logger = logger;
            _db = db;
            _cryptoService = cryptoService;
        }

        public IServiceConfig Generate<T>() where T : new()
        {
            var type = typeof(T);
            var processors = new Dictionary<Type, Func<IServiceConfig>>
            {
                {
                    typeof(HTTPProxy), delegate
                    {
                        var listeners = new List<Tuple<string, int>>();

                        var serviceConfig = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpListener);

                        if (serviceConfig.Count > 0)
                        {
                            foreach (var listener in serviceConfig)
                            {
                                var lstDict = JsonConvert
                                    .DeserializeObject<List<Dictionary<string, dynamic>>>(listener.Value)
                                    .FirstOrDefault();
                                if (lstDict != null && lstDict.ContainsKey("Item1") && lstDict.ContainsKey("Item2"))
                                {
                                    var ip = (string) lstDict["Item1"];
                                    var port = (int) lstDict["Item2"];
                                    listeners.Add(Tuple.Create(ip, port));
                                }
                                else
                                {
                                    _logger.LogError(
                                        "TG: Could not extract a valid ip:port pair from at least one listener.");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("TG: No listeners could be retrieved from the DB for " +
                                               typeof(HTTPProxy) + ", using defaults.");
                            listeners = Defaults.HTTP;
                        }

                        var proxyMode = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpMode)
                            .FirstOrDefault(); // Guaranteed to be one single value
                        var allowedDomains = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpAllowedDomains)
                            .FirstOrDefault(); // JSON list of strings, but there is only one list
                        var bannedDomains = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpBannedDomains)
                            .FirstOrDefault(); // JSON list of strings, but there is only one list

                        var actualMode = HTTPProxyModes.Normal;
                        var actualAllowedDomains = new List<string>();
                        var actualBannedDomains = new List<string>();

                        if (proxyMode != null)
                            if (proxyMode.Value == HTTPProxyModes.ExclusiveAllow.ToString())
                                actualMode = HTTPProxyModes.ExclusiveAllow;

                        if (allowedDomains != null)
                            actualAllowedDomains = JsonConvert
                                .DeserializeObject<List<string>>(allowedDomains.Value);

                        if (bannedDomains != null)
                            actualBannedDomains = JsonConvert
                                .DeserializeObject<List<string>>(bannedDomains.Value);

                        return new HTTPConfig(listeners, actualMode, actualAllowedDomains, actualBannedDomains);
                    }
                },
                {
                    typeof(OpenVPN), delegate
                    {
                        return new OpenVPNConfig(null, null);
                    }
                }
            };

            return processors[type]();
        }
    }
}