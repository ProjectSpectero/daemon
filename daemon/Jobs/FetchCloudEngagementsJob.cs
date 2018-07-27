using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Opaque.Responses;
using IRestClient = RestSharp.IRestClient;

namespace Spectero.daemon.Jobs
{
    public class FetchCloudEngagementsJob : IJob
    {
        private readonly IRestClient _restClient;
        private readonly IDbConnection _db;
        private readonly IIdentityProvider _identityProvider;
        private readonly ILogger<FetchCloudEngagementsJob> _logger;
        private readonly IMemoryCache _cache;
        private readonly AppConfig _config;
        private readonly ICloudHandler _cloudHandler;

        public FetchCloudEngagementsJob(IDbConnection db, IRestClient restClient,
            IIdentityProvider identityProvider, ILogger<FetchCloudEngagementsJob> logger,
            IMemoryCache cache, IOptionsMonitor<AppConfig> configMonitor,
            ICloudHandler cloudHandler)
        {
            _restClient = restClient;
            _db = db;
            _identityProvider = identityProvider;
            _logger = logger;
            _cache = cache;
            _config = configMonitor.CurrentValue;
            _cloudHandler = cloudHandler;
            
            logger.LogDebug("FCEJ: init successful, dependencies processed.");
        }

        public string GetSchedule()
        {
            var magicNumber = new Random().Next(2, 6);
            
            _logger.LogDebug($"FCEJ: decided to contact the Spectero Cloud once every {magicNumber} minute(s) to sync up.");
            
            return $"*/{magicNumber} * * * *"; // This sets it every 6 minutes, in cron expression.
        }

        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Perform()
        {
            if (! IsEnabled())
            {
                _logger.LogError("FCEJ: Job enabled, but matching criterion does not match. This should not happen, silently going back to sleep.");
                return;
            }

            var nodeId = ConfigUtils.GetConfig(_db, ConfigKeys.CloudConnectIdentifier).Result?.Value;
            var nodeKey = ConfigUtils.GetConfig(_db, ConfigKeys.CloudConnectNodeKey).Result?.Value;
            var localIdentity = _identityProvider.GetGuid().ToString();

            if (nodeId == null || nodeKey == null || localIdentity == null)
            {
                _logger.LogCritical("FCEJ: Job enabled, but one of -> (nodeId || nodeKey || localIdentity) were null. Please manually resolve.");
                return;
            }

            var slug = string.Format("unauth/node/{0}/config-pull", nodeId);
            var request = new RestRequest(slug, Method.POST) { RequestFormat = DataFormat.Json };

            var dataMap = new Dictionary<string, string>
            {
                {"node_key", nodeKey},
                {"install_id", localIdentity}
            };

            // RFC violating no more!
            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(dataMap), ParameterType.RequestBody);

            var response = _restClient.Execute(request);

            // Yes, can more or less throw unchecked exceptions. Hangfire will pick it up and mark the job as failed.
            if (response.ErrorException != null)
                throw response.ErrorException;

            /*
             * This is what we have to parse out of the response.
             * {
                   "errors": [],
                   "result": [
                       {
                           "engagement_id": 3,
                           "username": "0fe421935462b8fcfd99d30df00610f7",
                           "password": "$2y$10$flUlsbAED\/h7YvDLInBlguCBhE3UJ8CXf1sG\/s.fcH8YISHUlwJtO",
                           "sync_timestamp": "2018-03-22 23:24:27"
                       }
                   ],
                   "message": null,
                   "version": "v1"
               }
             */

            _logger.LogDebug("FCEJ: Got response from the cloud backend!: " + response.Content);

            if (response.Content.IsNullOrEmpty())
            {
                _logger.LogError("FCEJ: Got empty or null response from the backend cloud API, this is not meant to happen!");
                return;
            }

            // World's unsafest cast contender? Taking bets now.
            var parsedResponse = JsonConvert.DeserializeObject<CloudAPIResponse<List<Engagement>>>(response.Content);
            var engagements = parsedResponse.result;

            if (engagements == null)
            {
                _logger.LogWarning("FCEJ: List returned by the cloud for engagements was null. This is not meant to happen.");
                return;
            }

            // First, let's remove those users who are no longer in the output.
            // To do so, let's fetch ALL Cloud users (except the cloud administrative user, we're not messing with it)
            var currentCloudUsers = _db.Select<User>(x => x.Source == User.SourceTypes.SpecteroCloud
                                                          && x.AuthKey != AppConfig.CloudConnectDefaultAuthKey); 

            var usersToAllowToPersist =
                currentCloudUsers.Where(x => engagements.FirstOrDefault(f => f.username == x.AuthKey) != null);

            foreach (var userToBeRemoved in currentCloudUsers.Except(usersToAllowToPersist))
            {
                _logger.LogDebug("FCEJ: Terminating " + userToBeRemoved.AuthKey + " because cloud no longer has a reference to it.");
                AuthUtils.ClearUserFromCacheIfExists(_cache, userToBeRemoved.AuthKey); // Order matters, let's pay attention
                _db.Delete(userToBeRemoved);
            }


            foreach (var engagement in engagements)
            {
                /*
                 * First see if user already exists, and if details are different. If yes, look it up, and replace it fully.
                 * If not, we insert a brand new user.
                 */
                
                try
                {
                    var existingUser = _db.Single<User>(x => x.EngagementId == engagement.engagement_id);
                    if (existingUser != null)
                    {
                        if (existingUser.AuthKey == engagement.username &&
                            existingUser.Password == engagement.password &&
                            existingUser.Cert == engagement.cert &&
                            existingUser.CertKey == engagement.cert_key) continue;

                        AuthUtils.ClearUserFromCacheIfExists(_cache, existingUser.AuthKey); // Stop allowing cached logins, details are changing.

                        _logger.LogDebug("FCEJ: Updating user " + existingUser.AuthKey + " with new details.");

                        existingUser.AuthKey = engagement.username;
                        existingUser.Password = engagement.password; // This time it's already encrypted
                        existingUser.Cert = engagement.cert;
                        existingUser.CertKey = engagement.cert_key;
                        existingUser.CloudSyncDate = DateTime.Now;

                        _db.Update(existingUser);
                    }
                    else
                    {
                        _logger.LogDebug("FCEJ: Adding new user " + engagement.username + " as directed by the cloud.");
                        var user = new User
                        {
                            EngagementId = engagement.engagement_id,
                            AuthKey = engagement.username,
                            Password = engagement.password, // It already comes encrypted, don't use the setter to double encrypt it.
                            Source = User.SourceTypes.SpecteroCloud,
                            Roles = new List<User.Role> { User.Role.HTTPProxy, User.Role.OpenVPN, User.Role.SSHTunnel, User.Role.ShadowSOCKS }, // Only service access roles, no administrative access.
                            CreatedDate = DateTime.Now,
                            Cert = engagement.cert,
                            CertKey = engagement.cert_key, // TODO: The backend currently returns empty strings for these, but one day it'll be useful.
                            FullName = "Spectero Cloud User",
                            EmailAddress = "cloud-user@spectero.com",
                            CloudSyncDate = DateTime.Now
                        };

                        _db.Insert(user);
                    }

                }
                catch (DbException e)
                {
                    _logger.LogError(e, "FCEJ: Persistence failed!");
                    throw; // Throw to let Hangfire know that the job failed.
                }               
            }
        }

        public bool IsEnabled()
        {
            return _cloudHandler.IsConnected().Result; // Async sadness :(
        }
    }
}