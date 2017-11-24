using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Core.HTTP
{
    public class APIResponse
    {
        /* POCO that holds the components that make up a response from the API
         * 'Message' is derived from the status code, or optionally can be provided
         */


        public static APIResponse Create(HttpStatusCode code, object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            return new APIResponse(code, result, errors, message);
        }

        private APIResponse(HttpStatusCode code, object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            Code = code;
            Result = result;
            Errors = errors;
            Message = message;
        }


        public HttpStatusCode Code { get; set; }

        public IEnumerable<string> Errors { get; set; }

        public object Result { get; set; }

        private string ResolveMessage(HttpStatusCode code)
        {
            return "";
        }

        public string Message;

        public string Version => "1.0";
    }
}