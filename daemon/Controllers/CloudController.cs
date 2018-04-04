using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Jobs;
using Spectero.daemon.Libraries;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Requests;
using Spectero.daemon.Models.Responses;
using IRestClient = RestSharp.IRestClient;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(CloudController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CloudController : BaseController
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly IOutgoingIPResolver _ipResolver;
        private readonly IRestClient _restClient;
        private readonly string _cloudUserName;
        private readonly FetchCloudEngagementsJob _backgroundCloudEngagementsJob;

        private bool _restartNeeded;

        public CloudController(IOptionsSnapshot<AppConfig> appConfig, ILogger<CloudController> logger,
            IDbConnection db, IIdentityProvider identityProvider,
            IOutgoingIPResolver outgoingIpResolver, IRestClient restClient,
            IEnumerable<IJob> jobs)
            : base(appConfig, logger, db)
        {
            _identityProvider = identityProvider;
            _ipResolver = outgoingIpResolver;
            _cloudUserName = AppConfig.CloudConnectDefaultAuthKey;
            _restClient = restClient;
            _backgroundCloudEngagementsJob = jobs.FirstOrDefault(x => x.GetType() == typeof(FetchCloudEngagementsJob)) as FetchCloudEngagementsJob;            
        }

        [HttpGet("descriptor", Name = "GetLocalSystemConfig")]
        public async Task<IActionResult> GetDescriptor()
        {
            var output = new Dictionary<string, object>
            {
                {"config", AppConfig.ToObjectDictionary()},
                {"identity", _identityProvider.GetGuid().ToString()},
                { "status", await CompileCloudStatus() }
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

        private async Task<Dictionary<string, object>> CompileCloudStatus()
        {
            // We DRY now fam
            var status = await GetConfig(ConfigKeys.CloudConnectStatus);
            var identifier = await GetConfig(ConfigKeys.CloudConnectIdentifier);
            var nodeKey = await GetConfig(ConfigKeys.CloudConnectNodeKey);

            var responseDict = new Dictionary<string, object>
            {
                { ConfigKeys.CloudConnectStatus, bool.Parse(status?.Value) },
                { ConfigKeys.CloudConnectIdentifier, identifier?.Value },
                { ConfigKeys.CloudConnectNodeKey, nodeKey?.Value },
                { "app.version", AppConfig.version },
                { "app.restart.required", _restartNeeded }
            };

            return responseDict;
        }

        [HttpGet(Name = "GetCloudConnectStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> ShowStatus()
        {
            // What is DRY? ;V - TODO: fix this once we have global exception handling in the HTTP pipeline working
            if (!Request.HttpContext.Connection.RemoteIpAddress.IsLoopback())
                _response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, "");

            if (HasErrors())
                return StatusCode(403, _response);

            _response.Result = await CompileCloudStatus();
            return Ok(_response);
        }

        [HttpPost("manual", Name = "ManuallyConnectToSpecteroCloud")]
        [AllowAnonymous]
        public async Task<IActionResult> ManualCloudConnect([FromBody] ManualCloudConnectRequest connectRequest)
        {
            if (await CloudUtils.IsConnected(Db)
                && ! connectRequest.force)
            {
                // TODO: Bruh, we're connected
                _response.Errors.Add(Errors.CLOUD_ALREADY_CONNECTED, true);
                _response.Errors.Add(Errors.FORCE_PARAMETER_REQUIRED, true);
                return BadRequest(_response);
            }

            // What is DRY? ;V - TODO: fix this once we have global exception handling in the HTTP pipeline working
            if (!Request.HttpContext.Connection.RemoteIpAddress.IsLoopback())
                _response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, "");

            if (HasErrors())
                return StatusCode(403, _response);

            // Well ok, let's get it over with.
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectStatus, true.ToString());
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectIdentifier, connectRequest.NodeId.ToString());
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectNodeKey, connectRequest.NodeKey);

            ManageBackgroundJob();

            return await ShowStatus();
        }

        [HttpPost("disconnect", Name = "DisconnectFromSpecteroCloud")]
        [AllowAnonymous]
        public async Task<IActionResult> Disconnect()
        {
            // What is DRY? ;V - TODO: fix this once we have global exception handling in the HTTP pipeline working
            if (!Request.HttpContext.Connection.RemoteIpAddress.IsLoopback())
                _response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, "");

            if (! await CloudUtils.IsConnected(Db))
                _response.Errors.Add(Errors.CLOUD_NOT_CONNECTED, "");

            if (HasErrors())
                return StatusCode(403, _response);

            await DeleteConfigIfExists(ConfigKeys.CloudConnectNodeKey);
            await DeleteConfigIfExists(ConfigKeys.CloudConnectIdentifier);
            await CreateOrUpdateConfig(ConfigKeys.CloudConnectStatus, false.ToString());

            ManageBackgroundJob("disconnect");

            return await ShowStatus();
        }

        // This allows anonymous, but only from the local loopback.
        [HttpPost("connect", Name = "ConnectToSpecteroCloud")]
        [AllowAnonymous]
        public async Task<IActionResult> CloudConnect([FromBody] CloudConnectRequest connectRequest)
        {
            if (! ModelState.IsValid || connectRequest.NodeKey.IsNullOrEmpty())
                _response.Errors.Add(Errors.VALIDATION_FAILED, "FIELD_REQUIRED:NodeKey");

            // What is DRY? ;V
            if (! Request.HttpContext.Connection.RemoteIpAddress.IsLoopback())
                _response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, "");

            if (HasErrors())
                return StatusCode(403, _response);

            // First check is to verify that we aren't already connected
            // TODO: Use CloudUtils.
            var storedConfig = await Db
                .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectStatus);

            var processedKey = bool.TryParse(storedConfig?.Value, out var result) && result;
            if (processedKey)
            {
                // System is cloud connected, trying to connect again makes no sense
                // Let's look up its system ID in the Cloud Portal and cool-ly return a link
                var nodeIdConfig = await Db
                    .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectIdentifier);

                _response.Errors.Add(Errors.CLOUD_ALREADY_CONNECTED, nodeIdConfig?.Value);
                return StatusCode(403, _response);
            }

            // Ok, we aren't already connected. Let's go try talking to the backend and set ourselves up.
            var request = new RestRequest("unauth/node", Method.POST) {RequestFormat = DataFormat.Json};

            var generatedPassword = PasswordUtils.GeneratePassword(24, 0);
            var body = new UnauthNodeAddRequest {InstallId = _identityProvider.GetGuid().ToString()};

            var ownIp = await _ipResolver.Resolve();
            body.Ip = ownIp.ToString();

            body.Port = 6024; // TODO: read your own port and use that.
            body.Protocol = "http"; // TODO: figure out own listening protocol, use that.

            body.NodeKey = connectRequest.NodeKey;
            body.AccessToken = _cloudUserName + ":" + generatedPassword;  

            // Ok, we got the user created. Everything is ready, let's send off the request.
            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(body), ParameterType.RequestBody);

            var response = _restClient.Execute(request);


            if (response.ErrorException != null)
            {
                Logger.LogError(response.ErrorException, "CC: Connect attempt to the Spectero Cloud failed!");
                _response.Errors.Add(Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, response.ErrorMessage);
                return StatusCode(503, _response);
            }

            CloudAPIResponse<Node> parsedResponse;
            try
            {
                // Parse after error checking.
                parsedResponse = JsonConvert.DeserializeObject<CloudAPIResponse<Node>>(response.Content);
            }
            catch (JsonReaderException e)
            {
                // The Cloud Backend fed us bogus stuff, let's bail.
                Logger.LogError(e, "CC: Connect attempt to the Spectero Cloud failed!");
                _response.Errors.Add(Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, e.Message);
                return StatusCode(503, _response);
            }


            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    // Check if the cloud connect user exists already.
                    var user = await Db.SingleAsync<User>(x => x.AuthKey == _cloudUserName) ?? new User();

                    user.AuthKey = _cloudUserName;
                    user.PasswordSetter = generatedPassword;
                    user.EmailAddress = _cloudUserName + $"@spectero.com";
                    user.FullName = "Spectero Cloud Management User";
                    user.Roles = new List<User.Role> { Models.User.Role.SuperAdmin };
                    user.Source = Models.User.SourceTypes.SpecteroCloud;
                    user.CreatedDate = DateTime.Now;
                    user.CloudSyncDate = DateTime.Now;

                    // Checks if user existed already, or is being newly created.
                    if (user.Id != 0L)
                        await Db.UpdateAsync(user);
                    else
                        await Db.InsertAsync(user);

                    await CreateOrUpdateConfig(ConfigKeys.CloudConnectStatus, true.ToString());
                    await CreateOrUpdateConfig(ConfigKeys.CloudConnectIdentifier, parsedResponse.result.id.ToString());
                    await CreateOrUpdateConfig(ConfigKeys.CloudConnectNodeKey, connectRequest.NodeKey);

                   
                    _response.Result = parsedResponse;

                    ManageBackgroundJob();                                            

                    break;
                default:
                    // Likely a 400 or a 409, just show the response as is.
                    _response.Errors.Add(Errors.RESPONSE_CODE, response.StatusCode);
                    _response.Errors.Add(Errors.NODE_PERSIST_FAILED, parsedResponse.errors);
                    break;
            }


            _response.Message = Messages.DAEMON_RESTART_NEEDED;
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