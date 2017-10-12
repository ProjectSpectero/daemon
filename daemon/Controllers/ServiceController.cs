﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
{
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    public class ServiceController : BaseController
    {
        private readonly IServiceManager _serviceManager;
        private readonly string[] validActions = {"start", "stop", "restart"};

        private readonly string[] validServices = {"proxy", "vpn", "ssh"};

        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IStatistician statistician)
            : base(appConfig, logger, db)
        {
            _serviceManager = serviceManager;
        }


        [HttpGet("{name}/{task}", Name = "ManageServices")]
        public IEnumerable<string> Manage(string name, string task)
        {
            Logger.LogDebug("Service manager n -> " + name + ", a -> " + task);

            if (validServices.Any(s => name == s) &&
                validActions.Any(s => task == s))
            {
                _serviceManager.Process(name, task);
                return new[] {name + " was " + task + "ed successfully"};
            }
            throw new EInvalidRequest();
        }

        // POST api/service
        [HttpPost]
        public IEnumerable<string> Set()
        {
            Logger.LogInformation("SERVICE -> POST endpoint request");


            if (!Db.TableExists<User>())
                Db.CreateTable<User>();

            Db.Insert(
                new User {Id = 1, AuthKey = "A", CreatedDate = DateTime.Now}
            );

            var rows = Db.Select<User>().Count;

            return new[] {"Rows: " + rows};
        }
    }
}