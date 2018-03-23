using System;
using Microsoft.Extensions.DependencyInjection;
using NClap.Repl;
using RestSharp;

namespace Spectero.daemon.CLI
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // TODO: Make the event loop actually aware of the container.

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IRestClient>(c =>
                    new RestClient("http://127.0.0.1:6024/v1")
                )
                .BuildServiceProvider();

            var eventLoop = new Loop(typeof(Commands.Commands));
            eventLoop.Execute();
        }
    }
}
