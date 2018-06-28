using System.Collections.Immutable;

namespace Spectero.daemon.Models.Opaque
{
    public class OpaqueBase : IOpaqueModel
    {
        public string FormatValidationError(string errorKey, string field, string data = null)
        {
            return errorKey + ":" + field + ":" + data;
        }

        public bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            throw new System.NotImplementedException();
        }
    }
}