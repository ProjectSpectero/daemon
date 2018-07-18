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