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
                
                return (int) result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed M! ${e}");
                Console.WriteLine("You may wish to consider submitting a bug report if you believe this error to be a bug.");
                
                return commandErrorCode;
            }
            
        }
    }
}
