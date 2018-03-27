using System;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace Spectero.daemon.CLI
{
    public class Startup
    {
        private static IServiceProvider serviceProvider;

        public static IServiceProvider GetServiceProvider()
        {
            if (serviceProvider != null)
                return serviceProvider;

            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IRestClient>(c =>
                    new RestClient("http://127.0.0.1:6024/v1") // Be dynamic, read this off the env file.
                );

            serviceProvider = serviceCollection.BuildServiceProvider();

            return serviceProvider;
        }
    }
}