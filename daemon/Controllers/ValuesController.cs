using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly AppConfig _appConfig;

        public ValuesController(IOptionsSnapshot<AppConfig> appConfig)
        {
            _appConfig = appConfig.Value;
        }
        
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2", _appConfig.Key };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
           throw new NotImplementedException();
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
