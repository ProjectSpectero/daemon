﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;
using IService = ServiceStack.IService;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]

    public class ServiceController : BaseController
    {

        private string[] validServices = new string[] { "proxy", "vpn", "ssh" };
        private string[] validActions = new string[] { "start", "stop", "restart" };
        private readonly IServiceManager _serviceManager;
        
        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnectionFactory dbConnectionFactory, IServiceManager serviceManager)
            : base (appConfig, logger, dbConnectionFactory)
        {
            _serviceManager = serviceManager;
        }
        
        
        [HttpGet("{name}/{task}", Name = "ManageServices")]
        public IEnumerable<string> Manage (string name, string task)
        {
            Logger.LogDebug("Service manager n -> " + name + ", a -> " + task);

            if (validServices.Any(s => name == s) &&
                validActions.Any(s => task == s))
            {
                _serviceManager.Process(name, task);
                return new string[] { name + " was " + task + "ed successfully" };
            }
            else
               throw new EInvalidRequest();   
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