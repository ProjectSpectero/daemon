using System;
using System.Collections.Generic;

namespace Spectero.daemon.Models.Responses
{
    public class CloudAPIResponse
    {
        public List<string> errors;
        public object result;
        public string message;
        public string version;
    }
}