﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.APM;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Opaque.Requests;
using Spectero.daemon.Models.Opaque.Responses;

namespace Spectero.daemon.Libraries.CloudConnect
{
    public class CloudHandler : ICloudHandler
    {
        private readonly ILogger<CloudHandler> _logger;
        private readonly IDbConnection _db;
        private readonly IIdentityProvider _identityProvider;
        private readonly IOutgoingIPResolver _ipResolver;
        private readonly Apm _apm;
        private readonly IRestClient _restClient;
        
        private readonly string _defaultCloudUserName = AppConfig.CloudConnectDefaultAuthKey ?? "cloud";
        
        public CloudHandler(ILogger<CloudHandler> logger, IDbConnection dbConnection,
                            IIdentityProvider identityProvider, IOutgoingIPResolver ipResolver,
                            Apm apm, IRestClient restClient)
        {
            _logger = logger;
            _db = dbConnection;
            _identityProvider = identityProvider;
            _ipResolver = ipResolver;
            _apm = apm;
            _restClient = restClient;
        }
        
        public async Task<bool> IsConnected ()
        {
            var cloudConnectStatusConfig = await ConfigUtils.GetConfig(_db, ConfigKeys.CloudConnectStatus);

            return bool.TryParse(cloudConnectStatusConfig?.Value, out var result) && result;
        }

        public async Task<(bool success, Dictionary<string, object> errors,
            HttpStatusCode suggestedStatusCode, CloudAPIResponse<Node> cloudResponse)>
            Connect(HttpContext httpContext, string nodeKey)
        {
            var errors = new Dictionary<string, object>();
            
            // Ok, we aren't already connected. Let's go try talking to the backend and set ourselves up.
            var request = new RestRequest("unauth/node", Method.POST) {RequestFormat = DataFormat.Json};

            var generatedPassword = PasswordUtils.GeneratePassword(24, 0);
            var body = new UnauthNodeAddRequest {InstallId = _identityProvider.GetGuid().ToString()};

            var ownIp = await _ipResolver.Resolve();
            body.Ip = ownIp.ToString();

            body.Port = httpContext.Connection.LocalPort;

            body.Protocol = "http"; // TODO: When HTTPs support lands, use -> httpContext.Request.Protocol.ToLower() which returns things like http/1.1 (needs further parsing);

            body.NodeKey = nodeKey;
            body.AccessToken = _defaultCloudUserName + ":" + generatedPassword;

            // This is data about *THIS* specific system being contributed to the cloud/CRM.
            body.SystemData = _apm.GetAllDetails();
            body.Version = AppConfig.version;

            // Ok, we got the user created. Everything is ready, let's send off the request.
            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(body), ParameterType.RequestBody);

            var response = _restClient.Execute(request);


            if (response.ErrorException != null)
            {
                _logger.LogError(response.ErrorException, "CC: Connect attempt to the Spectero Cloud failed!");
                
                errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, response.ErrorMessage);
                
                return (false, errors, HttpStatusCode.ServiceUnavailable, null);
            }

            CloudAPIResponse<Node> parsedResponse = null;
            
            try
            {
                // Parse after error checking.
                parsedResponse = JsonConvert.DeserializeObject<CloudAPIResponse<Node>>(response.Content);
            }
            catch (JsonException e)

            {
                // The Cloud Backend fed us bogus stuff, let's bail.
                _logger.LogError(e, "CC: Connect attempt to the Spectero Cloud failed!");
                _logger.LogError("Cloud API said: " + response.Content);
                
                errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, e.Message);

                return (false, errors, HttpStatusCode.ServiceUnavailable, parsedResponse);
            }


            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    
                    // Check if the cloud connect user exists already.
                    var user = await _db.SingleAsync<User>(x => x.AuthKey == _defaultCloudUserName) ?? new User();

                    user.AuthKey = _defaultCloudUserName;
                    user.PasswordSetter = generatedPassword;
                    user.EmailAddress = _defaultCloudUserName + $"@spectero.com";
                    user.FullName = "Spectero Cloud Management User";
                    user.Roles = new List<User.Role> { Models.User.Role.SuperAdmin };
                    user.Source = Models.User.SourceTypes.SpecteroCloud;
                    user.CreatedDate = DateTime.Now;
                    user.CloudSyncDate = DateTime.Now;

                    // Checks if user existed already, or is being newly created.
                    if (user.Id != 0L)
                        await _db.UpdateAsync(user);
                    else
                        await _db.InsertAsync(user);
                    
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectStatus, true.ToString());
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectIdentifier, parsedResponse?.result.id.ToString());
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectNodeKey, nodeKey);                                         

                    break;
                
                default:
                    // Likely a 400 or a 409, just show the response as is.
                    errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, "");
                    errors.Add(Core.Constants.Errors.RESPONSE_CODE, response.StatusCode);
                    errors.Add(Core.Constants.Errors.NODE_PERSIST_FAILED, parsedResponse?.errors);
                    
                    _logger.LogError("Cloud API said: " + response.Content);
                    
                    return (false, errors, HttpStatusCode.ServiceUnavailable, parsedResponse);
            }

            return (true, errors, HttpStatusCode.OK, parsedResponse);
        }
    }
}