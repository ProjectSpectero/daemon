using System;
using System.Collections.Concurrent;

namespace Spectero.daemon.Libraries.Services
{
    public interface IServiceManager
    {
        string Process(string name, string action, out String error);
        ConcurrentDictionary<Type, IService> GetServices();
        IService GetService(Type type);
        void StopServices();
    }
}