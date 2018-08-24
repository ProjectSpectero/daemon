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
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using RestSharp;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.APM;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
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
        private readonly IMemoryCache _cache;
        private readonly ICryptoService _cryptoService;
        
        private readonly string _defaultCloudUserName = AppConfig.CloudConnectDefaultAuthKey ?? "cloud";
        
        public CloudHandler(ILogger<CloudHandler> logger, IDbConnection dbConnection,
                            IIdentityProvider identityProvider, IOutgoingIPResolver ipResolver,
                            Apm apm, IRestClient restClient,
                            IMemoryCache cache, ICryptoService cryptoService)
        {
            _logger = logger;
            _db = dbConnection;
            _identityProvider = identityProvider;
            _ipResolver = ipResolver;
            _apm = apm;
            _restClient = restClient;
            _cache = cache;
            _cryptoService = cryptoService;
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
            body.Version = AppConfig.Version;

            // Ok, we got the user created. Everything is ready, let's send off the request.
            var serializedBody = JsonConvert.SerializeObject(body);
            _logger.LogDebug($"We will be sending: {serializedBody}");
            
            request.AddParameter("application/json; charset=utf-8", serializedBody, ParameterType.RequestBody);
            
            // We have to ensure this user actually exists before sending off the request.
            // First, we need to remove any cached representation.
            AuthUtils.ClearUserFromCacheIfExists(_cache, _defaultCloudUserName);
                    
            // Check if the cloud connect user exists already.
            var user = await _db.SingleAsync<User>(x => x.AuthKey == _defaultCloudUserName) ?? new User();

            user.AuthKey = _defaultCloudUserName;
            user.PasswordSetter = generatedPassword;
            user.EmailAddress = _defaultCloudUserName + $"@spectero.com";
            user.FullName = "Spectero Cloud Management User";
            user.Roles = new List<User.Role> { User.Role.SuperAdmin };
            user.Source = User.SourceTypes.SpecteroCloud;
            user.CloudSyncDate = DateTime.Now;
            user.CertKey = PasswordUtils.GeneratePassword(48, 6);
            
            var userCertBytes = _cryptoService.IssueUserChain(user.AuthKey, new[] {KeyPurposeID.IdKPClientAuth}, user.CertKey);

            user.Cert = Convert.ToBase64String(userCertBytes);

            // Checks if user existed already, or is being newly created.
            if (user.Id != 0L)
                await _db.UpdateAsync(user);
            else
            {
                user.CreatedDate = DateTime.Now;
                await _db.InsertAsync(user);
            }

            var response = _restClient.Execute(request);


            if (response.ErrorException != null)
            {
                _logger.LogError(response.ErrorException, "CC: Connect attempt to the Spectero Cloud failed!");
                
                errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, response.ErrorMessage);
                
                await DeleteCloudUserIfExists();
                
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
                _logger.LogDebug("Cloud API said: " + response.Content);
                
                errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, e.Message);

                await DeleteCloudUserIfExists();

                return (false, errors, HttpStatusCode.ServiceUnavailable, parsedResponse);
            }


            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                                       
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectStatus, true.ToString());
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectIdentifier, parsedResponse?.result.id.ToString());
                    await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectNodeKey, nodeKey);                                         

                    break;
                
                default:
                    // Likely a 400 or a 409, just show the response as is.
                    errors.Add(Core.Constants.Errors.FAILED_TO_CONNECT_TO_SPECTERO_CLOUD, "");
                    errors.Add(Core.Constants.Errors.RESPONSE_CODE, response.StatusCode);
                    errors.Add(Core.Constants.Errors.NODE_PERSIST_FAILED, parsedResponse?.errors);
                    
                    _logger.LogDebug("Cloud API said: " + response.Content);

                    await DeleteCloudUserIfExists();
                    
                    return (false, errors, HttpStatusCode.ServiceUnavailable, parsedResponse);
            }

            return (true, errors, HttpStatusCode.OK, parsedResponse);
        }

        public async Task<bool> Disconnect()
        {
            // This key can NOT be deleted without causing in CloudController#CompileCloudStatus, toggle its state instead.
            await ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.CloudConnectStatus, false.ToString());
            
            // These are fair game to delete.
            await ConfigUtils.DeleteConfigIfExists(_db, ConfigKeys.CloudConnectIdentifier);
            await ConfigUtils.DeleteConfigIfExists(_db, ConfigKeys.CloudConnectNodeKey);

            await DeleteCloudUserIfExists();

            return true;
        }
        
        private async Task<bool> DeleteCloudUserIfExists()
        {
            var user = await _db.SingleAsync<User>(x => x.AuthKey == _defaultCloudUserName);

            if (user == null) return false;
            
            AuthUtils.ClearUserFromCacheIfExists(_cache, _defaultCloudUserName);
            await _db.DeleteAsync(user);

            return true;

        }
    }
}