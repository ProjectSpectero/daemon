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
            
            const int parseErrorCode = 125;
            const int commandErrorCode = 127;
            const int successReturnCode = 0;
            
            if (!CommandLineParser.TryParse(args, out cliArguments))
            {
                Console.WriteLine();
                Console.WriteLine("Could not parse the command-line, please check your input and retry.");
                
                return parseErrorCode;
            }

            try
            {
                var result = cliArguments.PrimaryCommand.Execute();
                
                return successReturnCode;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong :( " + e);
                Console.WriteLine("You may wish to consider submitting a bug report if you believe this error to be a bug.");
                
                return commandErrorCode;
            }
            
        }
    }
}
