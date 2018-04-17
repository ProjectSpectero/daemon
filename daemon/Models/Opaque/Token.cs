using System;
using System.Collections.Immutable;

namespace Spectero.daemon.Models.Opaque
{
    public class Token : IOpaqueModel
    {
        public string token { get; set; }

        public long expires { get; set; }

        public bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            throw new NotImplementedException();
        }
    }
}