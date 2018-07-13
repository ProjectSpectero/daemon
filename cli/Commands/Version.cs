using System;
using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public class Version : BaseJob
    {
        public override CommandResult Execute()
        {
            Console.WriteLine("Spectero Console v{0} with Spectero Daemon v{1}", AppConfig.version, daemon.Libraries.Config.AppConfig.version);
            Console.WriteLine("Copyright (c) 2017 - {0}, Spectero, Inc.", DateTime.Now.Year);

            return CommandResult.Success;
        }
        
        public override bool IsDataCommand() => true;
    }
}