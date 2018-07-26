using System.Collections.Immutable;
using System.Net;

namespace Spectero.daemon.Libraries.Errors
{
    public class ValidationError : BaseError
    {
        public ImmutableArray<string> Errors { get; }
        
        public ValidationError(ImmutableArray<string> errors) : base((int) HttpStatusCode.UnprocessableEntity,
            Core.Constants.Errors.VALIDATION_FAILED)
        {
            this.Errors = errors;
        }

        public ValidationError() : base((int) HttpStatusCode.UnprocessableEntity,
            Core.Constants.Errors.VALIDATION_FAILED)
        {
            
        }
    }
}