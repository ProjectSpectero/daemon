/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
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