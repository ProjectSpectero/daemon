using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using ServiceStack;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Services.OpenVPN;
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


    public class OpenVPNConfigUpdateRequest : BaseModel
    {
        public OpenVPNConfig commons;
        public List<OpenVPNListener> listeners;

        public bool Validate(out ImmutableArray<string> errors)
        {
            // TODO: @Andrew - validate the required properties for each property accordingly here.
            // I assume these are requests.

            IValitResult result = ValitRules<OpenVPNConfigUpdateRequest>
                .Create()

                // Ensure the commons array exists in the request.
                .Ensure(m => m.commons, _ => _
                    .Required()
                    .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "commons")))

                // Ensure the listeners array exists in the request.
                .Ensure(m => m.listeners, _ => _
                    .Required()
                    .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "listeners")))
                .For(this)
                .Validate();

            /*
             * Non-Standard Form Validaton
             * Anything beyond this point has been implemented due to a problem with the .Required()
             * Attribute above.
             */


            // Check if the multiple connections from same address attribute is undefined.
            if (commons.AllowMultipleConnectionsFromSameClient == null)
                throw new Exception(FormatValidationError(Errors.FIELD_REQUIRED, "commons.allowMultipleConnectionsFromSameClient"));

            // Check if the client to client attribute is undefined.
            if (commons.ClientToClient == null)
	            result.ErrorMessages.Append(FormatValidationError(Errors.FIELD_REQUIRED, "commons.clientToClient"));

            // TODO - (Paul/Andrew) - Revisit this later.
	        // Check if the maximum number of clients is defined, and if within a range of 10 <-> 4096.
            if (commons.MaxClients == null)
                throw new Exception(FormatValidationError(Errors.FIELD_INVALID, "commons.maxClients"));
            else if (commons.MaxClients < 10 || commons.MaxClients > 4096)
	            result.ErrorMessages.Append(FormatValidationError(Errors.FIELD_INVALID_RANGE, "commons.maxClients"));
	        
	        
            errors = result.ErrorMessages;
            return result.Succeeded;
        }
    }
}