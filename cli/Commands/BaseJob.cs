using System;
using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public abstract class BaseJob : SynchronousCommand
    {
        protected IServiceProvider ServiceProvider = Startup.GetServiceProvider();
    }
}