using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.HTTP.Filters;
using Spectero.daemon.Jobs;
using Spectero.daemon.Libraries.APM;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Opaque.Requests;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.HTTP.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(CloudController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CloudController : BaseController
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly Apm _apm;
        private readonly FetchCloudEngagementsJob _backgroundCloudEngagementsJob;
        private readonly ICloudHandler _cloudHandler;

        private bool _restartNeeded;

        public CloudController(IOptionsSnapshot<AppConfig> appConfig, ILogger<CloudController> logger,
            IDbConnection db, IIdentityProvider identityProvider,      
            IEnumerable<IJob> jobs, Apm apm,
            ICloudHandler cloudHandler)
            : base(appConfig, logger, db)
        {
            _identityProvider = identityProvider;
            _apm = apm;
            _backgroundCloudEngagementsJob = jobs.FirstOrDefault(x => x.GetType() == typeof(FetchCloudEngagementsJob)) as FetchCloudEngagementsJob;
            _cloudHandler = cloudHandler;
        }

        [HttpGet("descriptor", Name = "GetLocalSystemConfig")]
        public async Task<IActionResult> GetDescriptor()
        {
            var output = new Dictionary<string, object>
            {
                { "appSettings", AppConfig.ToObjectDictionary() },
                { "systemConfig", Db.Select<Configuration>() },
                { "identity", _identityProvider.GetGuid().ToString() },
                { "status", await CompileCloudStatus() },
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
            var res = new Dictionary<string, object> {{"status", "OK"}};

            _response.Result = res;
            
            return Ok(_response);
        }

        private async Task<Dictionary<string, object>> CompileCloudStatus()
        {
            // We DRY now fam
            var status = await GetConfig(ConfigKeys.CloudConnectStatus);
            var identifier = await GetConfig(ConfigKeys.CloudConnectIdentifier);
            var nodeKey = await GetConfig(ConfigKeys.CloudConnectNodeKey);
            
            var responseObject = new Dictionary<string, object>
            {
                { "cloud", new Dictionary<string, object>
                    {
                        { "status", bool.Parse(status?.Value) },
                        { "id", identifier?.Value },
                        { "nodeKey", nodeKey?.Value }
                    } 
                } ,
                {
                    "app", new Dictionary<string, object>
                    {
                        { "version", AppConfig.version },
                        { "restartNeeded", _restartNeeded },
                        { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production" }
                    }
                },
                {
                    "system", new Dictionary<string, object>
                    {
                        { "data", _apm.GetAllDetails() },
                    }
                }
            };

            return responseObject;
        }

        [HttpGet("remote", Name = "GetCloudConnectStatusRemotely")]
        public async Task<IActionResult> RemoteStatus()
        {
            _response.Result = await CompileCloudStatus();
            return Ok(_response);
        }

        [HttpGet(Name = "GetCloudConnectStatusLocally")]
        [AllowAnonymous]
        [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
        public async Task<IActionResult> LocalStatus()
        {
            _response.Result = await CompileCloudStatus();
            return Ok(_response);
        }

        [HttpPost("manual", Name = "ManuallyConnectToSpecteroCloud")]
        [AllowAnonymous]
        [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
        public async Task<IActionResult> ManualCloudConnect([FromBody] ManualCloudConnectRequest connectRequest)
        {
            if (await _cloudHandler.IsConnected() && ! connectRequest.force)
            {
                // Bruh, we're connected
                _response.Errors.Add(Errors.CLOUD_ALREADY_CONNECTED, true);
                _response.Errors.Add(Errors.FORCE_PARAMETER_REQUIRED, true);
                
                return BadRequest(_response);
            }

            // Well ok, let's get it over with.
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectStatus, true.ToString());
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectIdentifier, connectRequest.NodeId.ToString());
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectNodeKey, connectRequest.NodeKey);

            ManageBackgroundJob();

            return await LocalStatus();
        }

        [HttpPost("disconnect", Name = "DisconnectFromSpecteroCloud")]
        [AllowAnonymous]
        [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
        public async Task<IActionResult> Disconnect()
        {

            if (! await _cloudHandler.IsConnected())
                _response.Errors.Add(Errors.CLOUD_NOT_CONNECTED, "");

            if (HasErrors())
                return StatusCode(403, _response);

            await _cloudHandler.Disconnect();

            ManageBackgroundJob("disconnect");

            return await LocalStatus();
        }

        [HttpPost("connect", Name = "ConnectToSpecteroCloud")]
        [AllowAnonymous]
        [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
        public async Task<IActionResult> CloudConnect([FromBody] CloudConnectRequest connectRequest)
        {
            if (! ModelState.IsValid || connectRequest.NodeKey.IsNullOrEmpty())
                _response.Errors.Add(Errors.VALIDATION_FAILED, "FIELD_REQUIRED:NodeKey");

            if (HasErrors())
                return StatusCode(422, _response);

            // First check is to verify that we aren't already connected
            if (await _cloudHandler.IsConnected())
            {
                var nodeIdConfig = await GetConfig(ConfigKeys.CloudConnectIdentifier);
                
                _response.Errors.Add(Errors.CLOUD_ALREADY_CONNECTED, nodeIdConfig?.Value);
                return StatusCode(400, _response);
            }

            var connection = await _cloudHandler.Connect(HttpContext, connectRequest.NodeKey);

            if (!connection.success)
            {
                _response.Errors = connection.errors;

                Request.HttpContext.Response.Headers.Add(Headers.EUpstreamError, true.ToString());
                
                return StatusCode((int) connection.suggestedStatusCode, _response);
            }
            
            // OK bob, we succeeded. Let's par-tay.
            ManageBackgroundJob();
            _response.Result = connection.cloudResponse;
            _response.Message = Messages.CLOUD_CONNECTED_SUCCESSFULLY;
            
            return Ok(_response);
        }

        private void ManageBackgroundJob(string mode = "connect")
        {
            if (_backgroundCloudEngagementsJob != null)
            {
                var typeString = _backgroundCloudEngagementsJob.GetType().ToString();
                switch (mode)
                {
                    case "connect":
                        RecurringJob.AddOrUpdate(typeString, () => _backgroundCloudEngagementsJob.Perform(), _backgroundCloudEngagementsJob.GetSchedule);
                        break;

                    case "disconnect":
                        RecurringJob.RemoveIfExists(typeString);
                        break;
                    default:
                        throw new NotImplementedException("Unsupported mode (" + mode + ") given.");
                }
            }

            else
            {
                Logger.LogError("CC: Couuld not modify the background engagement update job, please restart. (" + mode + ")");
                _restartNeeded = true;
            }
        }
    }
}