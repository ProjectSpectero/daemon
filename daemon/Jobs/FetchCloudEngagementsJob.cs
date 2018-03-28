using System;
using System.Collections.Generic;
using System.Data;
using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Models.Responses;

namespace Spectero.daemon.Jobs
{
    public class FetchCloudEngagementsJob : IJob
    {
        private readonly IRestClient _restClient;
        private readonly IDbConnection _db;
        private readonly IIdentityProvider _identityProvider;
        private readonly ILogger<FetchCloudEngagementsJob> _logger;

        public FetchCloudEngagementsJob(IDbConnection db, IRestClient restClient,
            IIdentityProvider identityProvider, ILogger<FetchCloudEngagementsJob> logger)
        {
            _restClient = restClient;
            _db = db;
            _identityProvider = identityProvider;
            _logger = logger;
            logger.LogDebug("FCEJ init: successful, dependencies processed.");
        }

        public string GetSchedule()
        {
            return "*/6 * * * *"; // This sets it every 6 minutes, in cron expression.
        }

        [AutomaticRetry(Attempts = 1)]
        public void Perform()
        {
            if (!IsEnabled())
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
            var request = new RestRequest(slug, Method.GET) { RequestFormat = DataFormat.Json };

            var dataMap = new Dictionary<string, string>
            {
                {"node_key", nodeKey},
                {"install_id", localIdentity}
            };

            // RFC violating GET request with a body, w0w. The things we do for the MVP ;(
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

            // World's unsafest cast contender? Taking bets now.
            var parsedResponse = JsonConvert.DeserializeObject<CloudAPIResponse<List<Engagement>>>(response.Content);
            var engagements = parsedResponse.result;
            foreach (var engagement in engagements)
            {
                /*
                 * First see if user already exists, and if pw is different. If yes, look it up, and replace it fully.
                 * If not, we insert a brand new user.
                 * TODO: @alex (do ^), to learn how, see the UserController.
                 */
            }

            _logger.LogInformation(response.Content);

        }

        public bool IsEnabled()
        {
            return CloudUtils.IsConnected(_db).Result; // Async sadness :(
           
        }
    }
}