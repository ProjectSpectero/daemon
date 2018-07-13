using System;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;
using System.Collections.Generic;
using Polly;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Commands
{
    public class ConnectToSpecteroCloud : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "User's Node Key")]
        private string NodeKey { get; set; }
        
        [PositionalArgument(ArgumentFlags.Optional, Position = 1, Description = "Seconds We Should Retry Connect-ing for (for first-run)")]
        private int RetryForSeconds { get; set; }

        private int _retriedAttemptNumber = 1;
        
        public override bool IsDataCommand() => false;

        public override CommandResult Execute()
        {
            var request = new ConnectToCloudRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"nodeKey", NodeKey}
            };

            var result = CommandResult.RuntimeFailure;

            if (RetryForSeconds >= 11)
            {
                var policy = GenerateRetryPolicy(RetryForSeconds);
                try
                {
                    policy.Execute(() =>
                        result = HandleWithRetryEnabled(null, request, body, caller: this, throwsException: true));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed E! Despite retrying for {RetryForSeconds}s, we could not connect to the Spectero Cloud.");
                    return CommandResult.RuntimeFailure;
                }
                
            }
            else
                result = HandleRequest(null, request, body);

            return result;
        }

        private CommandResult HandleWithRetryEnabled(Action<APIResponse> action, IRequest request,
            Dictionary<string, object> requestBody = null, BaseJob caller = null,
            bool throwsException = false)
        {
            Console.WriteLine($"Connect (with Retry upto {RetryForSeconds}s) attempt: #{_retriedAttemptNumber} at {DateTime.UtcNow}");
            _retriedAttemptNumber++;
            
            return HandleRequest(null, request, requestBody, caller, throwsException);
        }

        private Policy GenerateRetryPolicy(int seconds, int delay = 10)
        {
            var timeoutAfterSeconds = Policy.Timeout(TimeSpan.FromSeconds(seconds));
            var retryEveryNSeconds =
                Policy.Handle<Exception>().WaitAndRetryForever(iteration => TimeSpan.FromSeconds(delay));

            return timeoutAfterSeconds.Wrap(retryEveryNSeconds);
        }

    }
}
