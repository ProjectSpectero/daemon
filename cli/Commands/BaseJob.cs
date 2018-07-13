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
            Dictionary<string, object> requestBody = null, BaseJob caller = null)
        {
            try
            {
                var response = request.Perform(requestBody);
                DisplayResult(response, caller);

                action?.Invoke(response);

                return CommandResult.Success;
            }
            catch (Exception e)
            {
                var message = AppConfig.Debug ? e.ToString() : e.Message;
                
                Console.WriteLine($"Failed! {message}");

                return CommandResult.RuntimeFailure;
            }
        }

        private static void DisplayResult(APIResponse response, BaseJob caller = null)
        {
            string output = null;
            
            if (response.Errors != null && response.Errors?.Count != 0)
            {
                Console.WriteLine("Failed!");
                foreach (var error in response.Errors)
                {
                    Console.WriteLine(error.Key + ":" + error.Value);
                }
            }
            else
            {
                var json = JsonConvert.SerializeObject(response.Result);
                var formattedJson = JToken.Parse(json).ToString(Formatting.Indented);
                
                if (caller != null && caller.IsDataCommand())
                {
                    // OK, dump the data.
                    output = formattedJson;
                }
                else
                {
                    // Just say that whatever was attempted actually succeeded.
                    output = AppConfig.Debug || AppConfig.OutputJson ? formattedJson : $"Success! Your requested task has completed as expected.";
                }
                
            }
            
            Console.WriteLine(output);
        }
    }
}