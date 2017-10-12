using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Config
{
    public interface IServiceConfigManager
    {
        IServiceConfig Generate<T>() where T : new();
    }
}
