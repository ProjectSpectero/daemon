/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System.Collections.Immutable;
using System.Linq;
using ServiceStack;
using Spectero.daemon.Libraries.Core.Constants;
using Valit;

namespace Spectero.daemon.Models.Opaque.Requests
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