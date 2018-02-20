using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(CloudController))]
    public class CloudController : BaseController
    {
        private readonly IIdentityProvider _identityProvider;

        public CloudController(IOptionsSnapshot<AppConfig> appConfig, ILogger<CloudController> logger,
            IDbConnection db, IIdentityProvider identityProvider)
            : base(appConfig, logger, db)
        {
            _identityProvider = identityProvider;
        }

        [HttpGet("descriptor", Name = "GetLocalSystemConfig")]
        public IActionResult GetDescriptor()
        {
            var output = new Dictionary<string, object>
            {
                {"config", AppConfig.ToObjectDictionary()},
                {"identity", _identityProvider.GetGuid().ToString()}
            };

            _response.Result = output;
            return Ok(_response);

        }

        [HttpGet("identity", Name = "GetLocalSystemIdentity")]
        public IActionResult GetIdentity()
        {
            _response.Result = _identityProvider.GetGuid().ToString();
            return Ok(_response);
        }

        [HttpGet("heartbeat", Name = "GetLocalSystemHeartbeat")]
        public IActionResult GetHeartbeat()
        {
            return Ok(_response);
        }

        [HttpPost("connect", Name = "ConnectToSpecteroCloud")]
        public async Task<IActionResult> CloudConnect()
        {
            // First check is to verify that we aren't already connected
            var storedConfig = await Db
                .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectStatus);

            var processedKey = bool.Parse(storedConfig.Value);

            if (processedKey)
            {
                // System is cloud connected, trying to connect again makes no sense
                // Let's look up its system ID in the Cloud Portal and cool-ly return a link
                var nodeIdConfig = await Db
                    .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectIdentifier);

                _response.Errors.Add(Errors.CLOUD_ALREADY_CONNECTED, nodeIdConfig?.Value);
                return StatusCode(403, _response);
            }

            _response.Result = storedConfig ?? throw new EInternalError();
            return Ok(_response);
        }
    }
}