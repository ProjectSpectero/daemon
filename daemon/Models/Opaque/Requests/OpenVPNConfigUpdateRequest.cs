using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public bool AllowMultipleConnectionsFromSameClient;
        public bool ClientToClient;
        public List<Tuple<DhcpOptions, string>> DhcpOptions;
        public int MaxClients;
        public List<string> PushedNetworks;
        public List<RedirectGatewayOptions> RedirectGateway;
        public List<OpenVPNListener> Listeners;

        public bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            // TODO: @Andrew - validate the required properties for each property accordingly here.
            // I assume these are requests.

            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

            IValitResult result = ValitRules<OpenVPNConfigUpdateRequest>
                .Create()
                // Ensure the listeners array exists in the request.
                .Ensure(m => m.Listeners, _ => _
                    .Required()
                    .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "listeners")))
                .For(this)
                .Validate();

            /*
             * Non-Standard Form Validaton
             *
             * Anything beyond this point has been implemented due to a problem with the .Required()
             * Attribute above.
             *
             * Commit all errors to the builder to produce an easy ImmutableArray<string>.
             * Return afterwards to reduce CPU cycles.
             */

            // Add the result's errors to the compiled builder array.
            foreach (var seedErrorMessage in result.ErrorMessages)
                builder.Add(seedErrorMessage);

            // Check if there's an error, if so return
            if (builder.Count != 0)
            {
                errors = builder.ToImmutable();
                return result.Succeeded;
            }

            // Check if the multiple connections from same address attribute is undefined.
            if (AllowMultipleConnectionsFromSameClient == null)
            {
                builder.Add(FormatValidationError(Errors.FIELD_REQUIRED, "commons.allowMultipleConnectionsFromSameClient"));
                errors = builder.ToImmutable();
                return result.Succeeded;
            }


            // Check if the client to client attribute is undefined.
            if (ClientToClient == null)
            {
                //TODO: Revisit and get a better understanding, find a way to properly start the array with value.
                builder.Add(FormatValidationError(Errors.FIELD_REQUIRED, "commons.commons.clientToClient"));
                errors = builder.ToImmutable();
                return result.Succeeded;
            }

            errors = builder.ToImmutable();
            return result.Succeeded;
        }
    }
}