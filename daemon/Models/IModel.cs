using System.Collections.Immutable;

namespace Spectero.daemon.Models
{
    public enum CRUDOperation
    {
        Create, Update
    }

    public interface IModel
    {
        bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create);
    }
}