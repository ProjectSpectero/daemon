using System.Collections.Immutable;
using System.Linq;
using ServiceStack;
using Spectero.daemon.Libraries.Core.Constants;
using Valit;

namespace Spectero.daemon.Models.Requests
{
    public class TokenRequest : BaseModel
    {
        public string AuthKey;
        public string Password;
        public string ServiceScope;

        // The operation param is useless here, for it is not used.
        public override bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            IValitResult result = ValitRules<TokenRequest>
                .Create()
                .Ensure(m => m.AuthKey, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "authKey")))
                .Ensure(m => m.Password, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "password")))
                .Ensure(m => m.ServiceScope, _ => _ 
                    .Satisfies(m => Defaults.ValidServices.Any(x => x == m)) // TODO: Validate that this works as intended.s
                        .WithMessage(FormatValidationError(Errors.FIELD_INVALID, "serviceScope"))
                        .When(m => ! m.ServiceScope.IsNullOrEmpty()))      
                .For(this)
                .Validate();

            errors = result.ErrorMessages;
            return result.Succeeded;
        }
    }
}