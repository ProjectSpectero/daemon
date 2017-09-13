using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Microsoft.Extensions.Logging;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly AppConfig _appConfig;
        private readonly ILogger<ValuesController> _logger;

        public ValuesController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ValuesController> logger)
        {
            _appConfig = appConfig.Value;
            _logger = logger;
        }
        
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.LogInformation("Log injection works!");
            _logger.LogError("Log Error works!");
            _logger.LogWarning("Log Warning works!");
            _logger.LogCritical("Log Critical works!");
            return new string[] { "value1", "value2", _appConfig.Key };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return id.ToString();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
