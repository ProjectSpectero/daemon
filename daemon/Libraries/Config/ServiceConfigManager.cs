using System;
using System.Collections.Generic;
using System.Net;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;

namespace Spectero.daemon.Libraries.Config
{
    public class ServiceConfigManager
    {
        public static IServiceConfig Generate<T>() where T : new()
        {
            var type = typeof(T);
            var processors = new Dictionary<Type, Func<IServiceConfig>>
            {
                {
                    typeof(HTTPProxy), delegate
                    {
                        var listeners = new Dictionary<IPAddress, int>
                        {
                            {IPAddress.Parse("127.0.0.1"), 6100}
                        };

                        return new HTTPConfig(listeners, HTTPProxyModes.Normal, null, null);
                    }
                }
            };

            return processors[type]();
        }
    }
}