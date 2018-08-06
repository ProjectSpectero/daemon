using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Spectero.daemon
{
    public class Program
    {
        public static void Main(string[] args)
        {            
            BuildWebHost(args).Run();
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return WebHost
                .CreateDefaultBuilder(args)
                .UseContentRoot(GetAssemblyLocation())
                .UseConfiguration(Startup.BuildConfiguration(environment))
                .UseStartup<Startup>()
                .Build();
        }

        public static string GetAssemblyLocation()
        {
            return Path.GetDirectoryName(
                Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)
            );
        }
    }
}