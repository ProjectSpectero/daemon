using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Medallion.Shell;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Spectero.daemon
{
    public class Program
    {
        private static string _sudoPath;

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

        public static string GetSudoPath()
        {
            // If the path hasn't previously been called, find it.
            if (_sudoPath == null)
            {
                var cmd = Command.Run("which", "sudo");
                _sudoPath = cmd.StandardOutput.ReadLine().First().ToString();
            }

            // Return the path to the sudo binary.
            return _sudoPath;
        }
    }
}