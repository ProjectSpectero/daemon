using System;
using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public class SetGlobalProperty : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "Name of the Global Property")]
        private string Property { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "User's proposed value")]
        private string Value { get; set; }

        public override CommandResult Execute()
        {
            switch (Property?.ToLower())
            {
                case "debug":
                    AppConfig.Debug = bool.Parse(Value);
                    break;
                    
                default:
                    Console.WriteLine("Unrecognized property " + Property + " given, ignoring...");
                    return CommandResult.UsageError;
            }

            Console.WriteLine("Property " + Property + " was set to " + Value);
            return CommandResult.Success;
        }
    }
}