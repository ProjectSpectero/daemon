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
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Spectero.daemon.Models.Opaque.Responses;

namespace Spectero.daemon.Libraries.CloudConnect
{
    public interface ICloudHandler
    {
        Task<bool> IsConnected ();

        Task<(bool success, Dictionary<string, object> errors,
                HttpStatusCode suggestedStatusCode, CloudAPIResponse<Node> cloudResponse)>
            Connect(HttpContext httpContext, string nodeKey);

        Task<bool> Disconnect();
    }
}