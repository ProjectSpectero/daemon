using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceStack.Text;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon
{
    public class Program
    {
       
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
            Utility.GetLocalRanges();
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
