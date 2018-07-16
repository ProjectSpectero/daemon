using System;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Spectero.daemon.CLI.Libraries.I18N;

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
                        new RestClient("http://127.0.0.1:6024/v1") // TODO: Be dynamic, read this off the env file.
                )
                .AddSingleton<I18NHandler>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            return ServiceProvider;
        }
    }
}