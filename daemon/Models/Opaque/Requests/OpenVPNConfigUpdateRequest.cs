using System.Collections.Generic;
using System.Collections.Immutable;
using ServiceStack;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Valit;

namespace Spectero.daemon.Models.Opaque.Requests
{
    public class OpenVPNConfigUpdateRequest : BaseModel
    {
        public OpenVPNConfig commons;
        public List<OpenVPNListener> listeners;

        public bool Validate(out ImmutableArray<string> errors)
        {
            // TODO: @Andrew - validate the required properties for each property accordingly here.
            IValitResult result = ValitRules<OpenVPNConfigUpdateRequest>
                .Create()
                .Ensure(m => m.commons, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "commons"))
                
                )
                .Ensure(m => m.listeners, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "listeners")))
                .For(this)
                .Validate();

            errors = result.ErrorMessages;
            return result.Succeeded;
        }

    }
}