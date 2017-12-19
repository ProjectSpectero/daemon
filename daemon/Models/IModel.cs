using System.Collections.Immutable;

namespace Spectero.daemon.Models
{
    public interface IModel
    {
        bool Validate(out ImmutableArray<string> errors);
    }
}