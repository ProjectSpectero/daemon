using System.Collections.Generic;
using System.Threading.Tasks;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public interface IRequest
    {
        APIResponse Perform(Dictionary<string, object> requestBody = null);
    }
}