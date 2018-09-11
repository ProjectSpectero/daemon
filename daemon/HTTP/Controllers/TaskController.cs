using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectero.daemon.HTTP.Filters;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Models.Opaque;
using Spectero.daemon.Models.Opaque.Requests;

namespace Spectero.daemon.HTTP.Controllers
{
    public class TaskDescriptor
    {
        // Unique identifier for this task.
        public string Id { get; set; }
        
        // The exact type of task that's being created
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskType Type { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskStatus Status { get; set; }
        
        // The payload in string form, this will be selectively parsed to get what we need out of it.
        public string Payload { get; set; }
        
        // If this is a task that requires managing an external process
        public CommandHolder Command { get; set; }
    }

    public enum TaskType
    {
        // ReSharper disable once InconsistentNaming
        ConnectOpenVPN,
        SetAsSystemProxy
    }

    public enum TaskStatus
    {
        PENDING,
        RUNNING,
        HALTED,
        FINISHED
    }
    
    [AllowAnonymous]
    [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(TaskController))]
    public class TaskController : BaseController
    {
        private readonly IProcessRunner _processRunner;
        private readonly ConcurrentDictionary<string, TaskDescriptor> _repository;
        
        private readonly string[] allowedManagementActions = new[] {"start", "stop"};
        
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
            // TODO: Need to implement this.
            throw new NotImplementedException();
        }

        [HttpPost("{id}/{action}")]
        public IActionResult Manage(string id, string action)
        {
            if (!_repository.TryGetValue(id, out var taskDescriptor))
                return NotFound();

            if (!allowedManagementActions.Any(x => x.Equals(action)))
                throw new ValidationError(
                    ImmutableArray.Create(OpaqueBase.FormatValidationError(Errors.FIELD_INVALID, "action",
                        action)));


            switch (taskDescriptor.Status)
            {
                    case TaskStatus.HALTED:
                    case TaskStatus.PENDING:
                    case TaskStatus.FINISHED:
                        if (action.Equals("stop"))
                            throw new DisclosableError(Errors.ILLEGAL_ACTION);
                        break;
                    
                    case TaskStatus.RUNNING:
                        if (action.Equals("start"))
                            throw new DisclosableError(Errors.ILLEGAL_ACTION);
                        break;
            }
            
            
            // TODO: Apply that "action" to the task.
            throw new NotImplementedException();
        }
        
        
        
        
    }
}