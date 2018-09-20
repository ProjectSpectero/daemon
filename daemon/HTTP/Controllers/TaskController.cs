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
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.HTTP.Filters;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Models.Opaque;
using Spectero.daemon.Models.Opaque.Requests;
using Spectero.daemon.Utilities.OpenVPN;

namespace Spectero.daemon.HTTP.Controllers
{
    [AllowAnonymous]
    [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(TaskController))]
    public class TaskController : BaseController
    {
        private readonly IProcessRunner _processRunner;
        private readonly ConcurrentDictionary<string, TaskDescriptor> _repository;

        private readonly string[] allowedManagementActions = {"start", "stop"};

        public TaskController(IOptionsSnapshot<AppConfig> appConfig, ILogger<TaskController> logger,
            IDbConnection db, IProcessRunner processRunner) : base(appConfig, logger, db)
        {
            _processRunner = processRunner;
            _repository = new ConcurrentDictionary<string, TaskDescriptor>();
        }

        // This is used to generate the task ID.
        private string GenerateTaskIdentifier(int len = 16)
        {
            return PasswordUtils.GeneratePassword(len, 0);
        }

        [HttpGet("{id}")]
        public IActionResult Show(string id)
        {
            if (_repository.TryGetValue(id, out var taskDescriptor))
                return Ok(taskDescriptor);

            return NotFound();
        }

        [HttpGet]
        public IActionResult List()
        {
            return Ok(_repository.Values);
        }

        [HttpPost]
        public IActionResult Create([FromBody] TaskCreationRequest creationRequest)
        {
            switch (creationRequest.Type)
            {
                case TaskType.ConnectToOpenVPNServer:
                    return Ok(ConnectToOpenVPNServer(creationRequest));

                case TaskType.SetAsSystemProxy:
                    throw new NotImplementedException("There is currently no implementation for the system proxy.");

                default:
                    return BadRequest("This request type cannot be handled.");
            }
        }

        [HttpPost("{id}/{requestedAction}")]
        public IActionResult Manage(string id, string requestedAction)
        {
            if (!_repository.TryGetValue(id, out var taskDescriptor))
                return NotFound();

            if (!allowedManagementActions.Any(x => x.Equals(requestedAction)))
                throw new ValidationError(
                    ImmutableArray.Create(OpaqueBase.FormatValidationError(Errors.FIELD_INVALID, "requestedAction",
                        requestedAction)));


            switch (taskDescriptor.Status)
            {
                case TaskStatus.Pending:
                case TaskStatus.Finished:
                    if (requestedAction.Equals("stop"))
                        throw new DisclosableError(Errors.ILLEGAL_ACTION);
                    break;

                case TaskStatus.Running:
                    if (requestedAction.Equals("start"))
                        throw new DisclosableError(Errors.ILLEGAL_ACTION);
                    break;
            }


            // TODO: Apply that "requestedAction" to the task.
            throw new NotImplementedException();
        }


        private TaskDescriptor ConnectToOpenVPNServer(TaskCreationRequest request)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), PasswordUtils.GeneratePassword(8, 0));
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            // Generate a GUID
            var guid = Guid.NewGuid();

            // Filenames
            var openvpnConfig = Path.Combine(tempDir, $"{guid}.ovpn");
            var openvpnConfigAuthfile = Path.Combine(tempDir, $"authfile-{guid}");

            // Write the configuration
            using (var ovpnConfig = new StreamWriter(openvpnConfig, false))
                ovpnConfig.Write(request.Payload.Config);

            // Write the authentication information
            using (var ovpnConfig = new StreamWriter(openvpnConfigAuthfile, false))
            {
                ovpnConfig.WriteLine(request.Payload.AuthKey);
                ovpnConfig.WriteLine(request.Payload.Password);
            }

            // Prepare an execution.
            var openvpnConfigurationProcOptions = new ProcessOptions()
            {
                Executable = OpenVPNUtils.DetermineBinaryPath(),
                Arguments = new[] {"--config", openvpnConfig, "--auth-user-pass", openvpnConfigAuthfile, "--auth-retry", "nointeract"},
                WorkingDirectory = tempDir,
                InvokeAsSuperuser = true,
                Monitor = true
            };

            // Run and get a command holder.
            var commandHolder = _processRunner.Run(openvpnConfigurationProcOptions);

            // Create a descriptor
            var descriptor = new TaskDescriptor()
            {
                Id = GenerateTaskIdentifier(),
                Status = TaskStatus.Running,
                Command = commandHolder,
                Type = TaskType.ConnectToOpenVPNServer,
                Payload = request.Payload
            };


            // Track
            _repository.TryAdd(descriptor.Id, descriptor);

            // Return
            return descriptor;
        }
    }
}