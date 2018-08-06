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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class AddRequestIdHeader : IMiddleware
    {
        /*
         * Heavily adapted from an example by Tugberk Ugurlu 
         * Credit: http://www.tugberkugurlu.com/archive/asp-net-5-and-log-correlation-by-request-id
         */

        private readonly RequestDelegate _next;

        public AddRequestIdHeader(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestIdFeature = context.Features.Get<IHttpRequestIdentifierFeature>();
            if (requestIdFeature?.TraceIdentifier != null)
            {
                context.Response.Headers["RequestId"] = requestIdFeature.TraceIdentifier;
            }

            await _next(context);
        }
    }
}