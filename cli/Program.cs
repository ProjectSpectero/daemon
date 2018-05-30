using System;
using NClap;
using Spectero.daemon.CLI.Commands.Arguments;

namespace Spectero.daemon.CLI
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            CliArguments cliArguments;

            if (!CommandLineParser.TryParse(args, out cliArguments))
            {
                Console.WriteLine();
                Console.WriteLine("Could not parse the command-line, please check your input and retry.");
                return;
            }

            try
            {
                var result = cliArguments.PrimaryCommand.Execute();
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
