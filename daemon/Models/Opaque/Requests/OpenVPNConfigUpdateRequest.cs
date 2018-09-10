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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Valit;

namespace Spectero.daemon.Models.Opaque.Requests
{
    /*
     Example incoming data.
     Use the following dataset below to get an idea of how this class should behave.
     
     {
		"commons": {
			"allowMultipleConnectionsFromSameClient": false,
			"clientToClient": false,
			"dhcpOptions": [],
			"maxClients": 1024,
			"pushedNetworks": [ "10.1.2.0/24" ],
			"redirectGateway": [
				"Def1"
			]
		},
		"listeners": [
			{
				"ipAddress": "1.2.3.4",
				"port": 1194,
				"protocol": "TCP",
				"managementPort": 15101,
				"network": "172.16.224.0/24"
			}
		]
	 }
     */


    public class OpenVPNConfigUpdateRequest : OpaqueBase
    {
        public bool? AllowMultipleConnectionsFromSameClient;
        public bool? ClientToClient;
	    public int? MaxClients;
	    
        public IEnumerable<Tuple<DhcpOptions, string>> DhcpOptions;
        public IEnumerable<string> PushedNetworks;
        public IEnumerable<RedirectGatewayOptions> RedirectGateway;
        public IEnumerable<OpenVPNListener> Listeners;

        public bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create,
	        bool throwsExceptions = false)
        {

            var builder = ImmutableArray.CreateBuilder<string>();

            var result = ValitRules<OpenVPNConfigUpdateRequest>
                .Create()
                // Ensure the listeners array exists in the request.
                .Ensure(m => m.Listeners, _ => _
                    .Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "listeners"))
					.MinItems(1)
						.WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "1")))
	            // IPAddress / IPNetwork validation, after this, no further "TryParse" is required.
	            .EnsureFor(m => m.Listeners, OpenVPNListener.validator)
	            .Ensure(m => m.Listeners, _ => _
					.Satisfies(x => x.Count() <= Defaults.DefinableOpenVPNListenerCount)
						.WithMessage(FormatValidationError(Errors.ENTITY_BOUND_REACHED, $"listeners:{Defaults.DefinableOpenVPNListenerCount}")))						
	            .Ensure(m => m.AllowMultipleConnectionsFromSameClient, _ => _
					.Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "allowMultipleConnectionsFromSameClient")))
	            .Ensure(m => m.ClientToClient, _ => _
		            .Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "clientToClient")))
	            .Ensure(m => m.MaxClients, _ => _
					.Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "maxClients"))
					.IsGreaterThanOrEqualTo(512)
						.WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "maxClients", "512"))
		            .IsLessThanOrEqualTo(2048)
						.WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "maxClients", "2048")))
				.Ensure(m => m.DhcpOptions, _ => _
					.Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "dhcpOptions")))
	            .Ensure(m => m.PushedNetworks, _ => _
					.Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "pushedNetworks")))
	            .EnsureFor(m => m.PushedNetworks, _ => _
					.Satisfies(x => IPNetwork.TryParse(x, out var _))
						.WithMessage(FormatValidationError(Errors.FIELD_INVALID, "pushedNetworks")))
	            .Ensure(m => m.RedirectGateway, _ => _
		            .Required()
						.WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "redirectGateway")))
                .For(this)
                .Validate();

	        
	        errors = result.ErrorMessages;
	        
	        if (! result.Succeeded && throwsExceptions)
				throw new ValidationError(errors);

	        return result.Succeeded;
	       
        }
    }
}