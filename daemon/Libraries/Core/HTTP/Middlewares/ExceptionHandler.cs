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
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Marshaling;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class ExceptionHandler : IMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandler> _logger;

        public ExceptionHandler(RequestDelegate next, ILogger<ExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            const string message = Constants.Errors.SOMETHING_WENT_WRONG;
            var code = (int) HttpStatusCode.InternalServerError;
            
            var _response = APIResponse.Create(null, new Dictionary<string, object> (), null);

            // This is one of our own defined errors, likely thrown by code we wrote
            if (exception is BaseError baseError)
            {
                
                // Alright, let's see which one you are exactly.
                code = baseError.Code;

                switch (baseError)
                {
                    case DisclosableError disclosableError:
                        _response.Errors.Add(disclosableError.key, disclosableError.Message);
                        break;
                    case ValidationError validationError:
                        _response.Errors.Add(validationError.Message, validationError.Errors);
                        break;
                    case InternalError internalError:
                        _logger.LogWarning(internalError, "Spectero defined internal error found.");
                        _response.Errors.Add(Constants.Errors.SOMETHING_WENT_WRONG, Constants.Errors.DETAILS_ABSTRACTED);
                        break;
                }
            }
            else
            {
                // OK, maybe some other generic exception. These are NOT supposed to happen, though.
                // Code and message as per defaults (not disclosed), but it WILL be logged.
                _logger.LogError(exception, "Unhandled exception found.");
                _response.Errors.Add(Constants.Errors.SOMETHING_WENT_WRONG, Constants.Errors.DETAILS_ABSTRACTED);
            }

            _response.Message = message;            
                      
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                
                context.Response.StatusCode = code;
                context.Response.ContentType = "application/json; charset=utf-8";
                
                await context.Response.WriteAsync(JsonConvert.SerializeObject(_response, new JsonSerializerSettings 
                { 
                    ContractResolver = new CamelCaseExceptDictionaryKeysResolver() 
                }));
            }
            else
            {
                _logger.LogWarning($"Could not return formatted response for exception of type {exception.GetType()} because response had already started.");
            }
        }
    }
}