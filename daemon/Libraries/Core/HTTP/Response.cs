using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Core.HTTP
{
    public class Response
    {
        /* POCO that holds the components that make up a response from the API
         * 'Message' is derived from the status code, or optionally can be provided
         */


        public static Response Create(HttpStatusCode code, object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            return new Response(code, result, errors, message);
        }

        protected Response(HttpStatusCode code, object result = null, IEnumerable<string> errors = null,
            string message = null)
        {
            Code = code;
            Result = result;
            Errors = errors;
            ProvidedMessage = message;
        }
         

        public HttpStatusCode Code { get; set; }

        public IEnumerable<string> Errors { get; set; }

        public object Result { get; set; }

        private string ProvidedMessage { get; set; }
        private string ResolveMessage(HttpStatusCode code)
        {
            return "";
        }
        public string Message => ProvidedMessage ?? ResolveMessage(Code);

        public string Version => "1.0";
    }
}