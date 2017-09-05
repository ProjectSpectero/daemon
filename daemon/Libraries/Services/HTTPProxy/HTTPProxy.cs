using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPProxy : IService
    {
        private HTTPConfig serviceConfig;

        public void Start(IServiceConfig serviceConfig)
        {
            this.serviceConfig = (HTTPConfig) serviceConfig;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
