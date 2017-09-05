using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Services
{
    interface IService
    {
        void Start (IServiceConfig serviceConfig);
        void Stop();
    }
}
