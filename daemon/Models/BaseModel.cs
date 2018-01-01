using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class BaseModel : IModel
    {
        [Index]
        [AutoIncrement]
        public long Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime UpdatedDate { get; set; }

        public virtual bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            throw new NotImplementedException();
        }

        public string FormatValidationError(string errorKey, string field, string data = null)
        {
            return errorKey + ":" + field + ":" + data;
        }
    }
}