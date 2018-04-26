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
            Console.WriteLine("Spectero Console v{0}", AppConfig.version);

            try
            {
                var eventLoop = new Loop(typeof(Commands.Commands));
                eventLoop.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong :( " + e);
                Console.WriteLine("You may wish to consider submitting a bug report if you believe this error to be a bug.");
                Console.ReadLine();
            }
            
        }
    }
}
