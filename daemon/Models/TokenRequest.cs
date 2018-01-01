using System.Collections.Immutable;
using Spectero.daemon.Libraries.Core.Constants;
using Valit;

namespace Spectero.daemon.Models
{
    public class TokenRequest : BaseModel
    {
        public string AuthKey;
        public string Password;

        // The operation param is useless here, for it is not used.
        public override bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            IValitResult result = ValitRules<TokenRequest>
                .Create()
                .Ensure(m => m.AuthKey, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "authKey"))
                    .Matches(@"^[a-zA-Z][\w]*$")
                    .WithMessage(FormatValidationError(Errors.FIELD_REGEX_MATCH, "authKey", @"^[a-zA-Z][\w]*$")))
                .Ensure(m => m.Password, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "password"))
                    .MinLength(5)
                        .WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "password", "5"))
                    .MaxLength(72)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "password", "72")))
                .For(this)
                .Validate();

            errors = result.ErrorMessages;
            return result.Succeeded;
        }
    }
}