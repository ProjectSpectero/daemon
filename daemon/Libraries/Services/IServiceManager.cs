using System;
using System.Collections.Concurrent;

namespace Spectero.daemon.Libraries.Services
{
    public interface IServiceManager
    {
        bool Process(string name, string action);
        ConcurrentDictionary<Type, IService> GetServices();

    }
}