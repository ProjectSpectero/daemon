using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Core.HTTP
{
    public class APIResponse
    {
        /* POCO that holds the components that make up a response from the API
         * 'Message' is derived from the status code, or optionally can be provided
         */


        public static APIResponse Create(object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            return new APIResponse(result, errors, message);
        }

        public APIResponse(object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            Result = result;
            Errors = errors;
            Message = message;
        }

        public IEnumerable<string> Errors { get; set; }

        public object Result { get; set; }

        public string Message { get; set; }

        public string Version => "1.0";
    }
}