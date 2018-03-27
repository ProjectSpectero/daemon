using System;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace Spectero.daemon.CLI
{
    public static class Startup
    {
        private static IServiceProvider ServiceProvider { get; set; }

        public static IServiceProvider GetServiceProvider()
        {
            if (ServiceProvider != null)
                return ServiceProvider;

            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IRestClient>(c =>
                    new RestClient("http://127.0.0.1:6024/v1") // Be dynamic, read this off the env file.
                );

            ServiceProvider = serviceCollection.BuildServiceProvider();

            return ServiceProvider;
        }
    }
}