using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Services
{
    public interface IService
    {
        void Start (IServiceConfig serviceConfig);
        void ReStart(IServiceConfig serviceConfig);
        void Stop();

        void LogState(string caller);
    }
}
