using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Config
{
    public class ServiceConfigManager : IServiceConfigManager
    {
        private readonly AppConfig _appConfig;
        private readonly IDbConnection _db;
        private readonly ILogger<ServiceConfigManager> _logger;

        public ServiceConfigManager(IOptionsMonitor<AppConfig> config, ILogger<ServiceConfigManager> logger,
            IDbConnection db)
        {
            _appConfig = config.CurrentValue;
            _logger = logger;
            _db = db;
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

                        var serviceConfig = _db.Select<Configuration>(x => x.Key == "http.listener");

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


                        return new HTTPConfig(listeners, HTTPProxyModes.Normal, null, null);
                    }
                }
            };

            return processors[type]();
        }
    }
}