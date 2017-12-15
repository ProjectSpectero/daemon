using System;
using System.Collections.Generic;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Config
{
    public interface IServiceConfigManager
    {
        IEnumerable<IServiceConfig> Generate(Type type);
    }
}