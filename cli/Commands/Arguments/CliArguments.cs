using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands.Arguments
{
    public class CliArguments
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0)]
        public CommandGroup<Commands> PrimaryCommand { get; set; }
    }
}