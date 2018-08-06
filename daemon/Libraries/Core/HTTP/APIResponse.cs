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