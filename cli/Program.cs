using System;
using NClap;
using Spectero.daemon.CLI.Commands.Arguments;

namespace Spectero.daemon.CLI
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            CliArguments cliArguments;

            if (!CommandLineParser.TryParse(args, out cliArguments))
            {
                Console.WriteLine();
                Console.WriteLine("Could not parse the command-line, please check your input and retry.");
                return 127;
            }

            try
            {
                var result = cliArguments.PrimaryCommand.Execute();
                
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong :( " + e);
                Console.WriteLine("You may wish to consider submitting a bug report if you believe this error to be a bug.");
                
                return 127;
            }
            
        }
    }
}
