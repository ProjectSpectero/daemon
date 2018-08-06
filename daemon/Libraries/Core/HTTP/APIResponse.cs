using System.Collections.Generic;

namespace Spectero.daemon.Libraries.Core.HTTP
{
    public class APIResponse
    {
        /* POCO that holds the components that make up a response from the API
         * 'Message' is derived from the status code, or optionally can be provided
         */


        public static APIResponse Create(object result = null, Dictionary<string, object> errors = null,
            string message = null)
        {
            return new APIResponse(result, errors, message);
        }

        public APIResponse(object result = null, Dictionary<string, object> errors = null,
            string message = null)
        {
            Result = result;
            Errors = errors;
            Message = message;
        }

        public Dictionary<string, object> Errors { get; set; }

        public object Result { get; set; }

        public string Message { get; set; }

        public double Version => 1.0;
    }
}