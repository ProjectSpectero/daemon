using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Requests;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(CloudController))]
    public class CloudController : BaseController
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly IOutgoingIPResolver _ipResolver;
        private readonly string cloudUserName;


        public CloudController(IOptionsSnapshot<AppConfig> appConfig, ILogger<CloudController> logger,
            IDbConnection db, IIdentityProvider identityProvider,
            IOutgoingIPResolver outgoingIpResolver)
            : base(appConfig, logger, db)
        {
            _identityProvider = identityProvider;
            _ipResolver = outgoingIpResolver;
            cloudUserName = "cloud";
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

        [HttpGet(Name = "GetCloudConnectStatus")]
        public async Task<IActionResult> ShowStatus()
        {
            return null;
        }

        // This allows anonymous, but only from the local loopback.
        [HttpPost("connect", Name = "ConnectToSpecteroCloud")]
        [AllowAnonymous]
        public async Task<IActionResult> CloudConnect([FromBody] CloudConnectRequest connectRequest)
        {
            if (! ModelState.IsValid || connectRequest.NodeKey.IsNullOrEmpty())
                _response.Errors.Add(Errors.VALIDATION_FAILED, "FIELD_REQUIRED:NodeKey");
            
            if (! Request.HttpContext.Connection.RemoteIpAddress.IsLoopback())
                _response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, "");

            if (HasErrors())
                return StatusCode(403, _response);

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

            // Ok, we aren't already connected. Let's go try talking to the backend and set ourselves up.
            var client = new RestClient(AppConfig.ApiBaseUri);
            var request = new RestRequest("unauth/node", Method.POST);
            request.RequestFormat = DataFormat.Json;

            var generatedPassword = PasswordUtils.GeneratePassword(24, 0);
            var body = new UnauthNodeAddRequest {InstallId = _identityProvider.GetGuid().ToString()};

            var ownIp = await _ipResolver.Resolve();
            body.Ip = ownIp.ToString();

            body.Port = 6024; // TODO: read your own port and use that.
            body.Protocol = "http"; // TODO: figure out own listening protocol, use that.

            body.NodeKey = connectRequest.NodeKey;
            body.AccessToken = cloudUserName + ":" + generatedPassword;

            // Check if the cloud connect user exists already.
            var user = await Db.SingleAsync<User>(x => x.AuthKey == cloudUserName) ?? new User();

            user.AuthKey = cloudUserName;
            user.PasswordSetter = generatedPassword;
            user.EmailAddress = cloudUserName + $"@spectero.com";
            user.FullName = "Spectero Cloud Management User";
            user.Roles = new List<User.Role>{ Models.User.Role.SuperAdmin };
            user.Source = Models.User.SourceTypes.SpecteroCloud;
            user.CreatedDate = DateTime.Now;
            user.CloudSyncDate = DateTime.Now;

            // Checks if user existed already, or is being newly created.
            if (user.Id != 0L)
                await Db.UpdateAsync(user);
            else
                await Db.InsertAsync(user);

            // Ok, we got the user created. Everything is ready, let's send off the request.
            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(body), ParameterType.RequestBody);


            var response = client.Execute(request);
            _response.Result = response.Content;

            return Ok(_response);
        }
    }
}