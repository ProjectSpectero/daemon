using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using Spectero.daemon.Libraries.Core.Constants;
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

        public bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
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
						.WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "512"))
		            .IsLessThanOrEqualTo(2048)
						.WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "2048")))
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

	        return result.Succeeded;
	       
        }
    }
}