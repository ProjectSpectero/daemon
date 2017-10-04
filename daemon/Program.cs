using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceStack.Text;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Services.HTTPProxy;

namespace Spectero.daemon
{
    public class Program
    {
       
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
            
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
