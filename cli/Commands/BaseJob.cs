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
using System.Text;
using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.CLI.Libraries.I18N;
using Spectero.daemon.CLI.Requests;
using Spectero.daemon.Libraries.Core.HTTP;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace Spectero.daemon.CLI.Commands
{
    public abstract class BaseJob : SynchronousCommand
    {
        protected readonly IServiceProvider ServiceProvider 
            = Startup.GetServiceProvider();
                
        public abstract bool IsDataCommand();

        protected CommandResult HandleRequest(Action<APIResponse> action, IRequest request,
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

        private void DisplayResult(APIResponse response, BaseJob caller = null, bool throwException = false)
        {
            var i18n = ServiceProvider.GetService<I18NHandler>();
            
            var isTranslationDisabled =
                (caller != null && caller.IsDataCommand()) || AppConfig.OutputJson || AppConfig.Debug;
            
            if (response?.Errors != null && response.Errors?.Count != 0)
            {
                Console.WriteLine("Failed DR!");
                foreach (var error in response.Errors)
                {
                    var builder = new StringBuilder();

                    if (isTranslationDisabled)
                        builder.Append($"{error.Key} {error.Value}");
                    else
                    {
                        if (!error.Key.IsNullOrEmpty())
                            builder.Append(i18n.get(error.Key) + " ");
                    
                        if (!error.Value.ToString().IsNullOrEmpty())
                            builder.Append(i18n.get(error.Value.ToString()));
                    }
                                        
                    Console.WriteLine(builder);
                }
                
                if (throwException)
                    throw new Exception("Spectero Cloud rejected the request, errors array was NOT empty!");
            }
            else
            {
                var json = JsonConvert.SerializeObject(response?.Result);
                var formattedJson = JToken.Parse(json).ToString(Formatting.Indented);
                
                var output = isTranslationDisabled ? formattedJson : $"Success! Your requested task has completed as expected.";
               
                Console.WriteLine(output);                
            }
            
        }
    }
}