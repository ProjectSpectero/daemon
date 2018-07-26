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
            
            context.Response.Clear();
            context.Response.StatusCode = code;

            context.Response.ContentType = "application/json; charset=utf-8";
                        
            await context.Response.WriteAsync(JsonConvert.SerializeObject(_response, new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCaseExceptDictionaryKeysResolver() 
            }));
            
        }
    }
}