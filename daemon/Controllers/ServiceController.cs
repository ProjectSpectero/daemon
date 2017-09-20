using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    public class ServiceController : BaseController
    {
        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger, IDbConnectionFactory dbConnectionFactory) : base (appConfig, logger, dbConnectionFactory)
        {
               
        }
        
        // GET api/service
        [HttpGet]
        public IEnumerable<string> Get()
        {
            Logger.LogInformation("Getting records!");
            return new string[] { Db.Select<User>().ToJson() };
        }
        
        // POST api/service
        [HttpPost]
        public IEnumerable<string> Set()
        {
            Logger.LogInformation("SERVICE -> POST endpoint request");
            
            
            if (! Db.TableExists<User>())
                Db.CreateTable<User>();
            
            Db.Insert(
                    new User { Id = 1, Name = "A", CreatedDate = DateTime.Now },
                    new User { Id = 2, Name = "B", CreatedDate = DateTime.Now },
                    new User { Id = 3, Name = "C", CreatedDate = DateTime.Now },
                    new User { Id = 4, Name = "C", CreatedDate = DateTime.Now }
            );

            var rows = Db.Select<User>().Count;
            
            return new string[] { "Rows: " + rows };
        }
        
    }
}