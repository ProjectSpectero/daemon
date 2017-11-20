using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Core.HTTP
{
    public class Response
    {
        /* POCO that holds the components that make up a response from the API
         * 'Message' is derived from the status code, or optionally can be provided
         *  [
         *      'code' -> this.Code,
         *      'Errors' -> null | [ "ERROR_CODE_1" ... ],
         *      'Data' -> null | [ ... ],
         *      'Message' -> "MESSAGE_CODE_1"
         *  ]
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
            Message = message;
        }
         

        public HttpStatusCode Code { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public object Result { get; set; }
        public string Message { get; set; }

        public string Version
        {
            get { return "1.0"; }
        }
        public string RequestId { get; }
    }
}