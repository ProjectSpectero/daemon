using System;
using System.Collections.Generic;
using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.CLI.Requests;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Commands
{
    public abstract class BaseJob : SynchronousCommand
    {
        protected readonly IServiceProvider ServiceProvider 
            = Startup.GetServiceProvider();
                
        public abstract bool IsDataCommand();

        protected static CommandResult HandleRequest(Action<APIResponse> action, IRequest request,
            Dictionary<string, object> requestBody = null, BaseJob caller = null,
            bool throwsException = false)
        {
            try
            {
                var response = request.Perform(requestBody);
                DisplayResult(response, caller, throwsException);

                action?.Invoke(response);

                return CommandResult.Success;
            }
            catch (Exception e)
            {                
                var message = AppConfig.Debug ? e.ToString() : e.Message;
                
                Console.WriteLine($"Failed HR! {message}");
                
                if (throwsException)
                    throw;

                return CommandResult.RuntimeFailure;
            }
        }

        private static void DisplayResult(APIResponse response, BaseJob caller = null, bool throwException = false)
        {            
            if (response.Errors != null && response.Errors?.Count != 0)
            {
                Console.WriteLine("Failed DR!");
                foreach (var error in response.Errors)
                {
                    Console.WriteLine(error.Key + ":" + error.Value);
                }
                
                if (throwException)
                    throw new Exception("Spectero Cloud rejected the request, errors array was NOT empty!");
            }
            else
            {
                var json = JsonConvert.SerializeObject(response.Result);
                var formattedJson = JToken.Parse(json).ToString(Formatting.Indented);
                
                var output = (caller != null && caller.IsDataCommand()) || AppConfig.OutputJson || AppConfig.Debug ? formattedJson : $"Success! Your requested task has completed as expected.";
               
                Console.WriteLine(output);                
            }
            
        }
    }
}