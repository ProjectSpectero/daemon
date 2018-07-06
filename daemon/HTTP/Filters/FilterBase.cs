using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.HTTP.Filters
{
    public class FilterBase : ActionFilterAttribute
    {
        protected APIResponse Response = APIResponse.Create(null, new Dictionary<string, object> (), null);
    }
}