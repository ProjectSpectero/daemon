using System;
using System.Collections.Generic;
using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Commands
{
    public abstract class BaseJob : SynchronousCommand
    {
        protected readonly IServiceProvider ServiceProvider 
            = Startup.GetServiceProvider();

        protected static void DisplayResult(APIResponse response)
        {
            if (response.Errors.Count != 0)
            {
                foreach (KeyValuePair<string, object> error in response.Errors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                string json = JsonConvert.SerializeObject(response.Result);

                string jsonFormatted = JValue.Parse(json).ToString(Formatting.Indented);

                Console.WriteLine(jsonFormatted);
            }
        }
    }
}