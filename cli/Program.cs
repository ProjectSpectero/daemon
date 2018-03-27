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
          

            
            var eventLoop = new Loop(typeof(Commands.Commands));
            eventLoop.Execute();
        }
    }
}
