using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorLight;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Models;

namespace Spectero.daemon.HTTP.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    public class DebugController : BaseController
    {

        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks = Utility.GetLocalRanges();
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IStatistician _statistician;
        private readonly IRazorLightEngine _engine;
        private readonly IIdentityProvider _identity;
        private readonly IProcessRunner _processRunner;

        public DebugController(IOptionsSnapshot<AppConfig> appConfig, ILogger<DebugController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IServiceConfigManager serviceConfigManager, IStatistician statistician,
            IIdentityProvider identityProvider, IRazorLightEngine engine,
            IProcessRunner processRunner)
            : base(appConfig, logger, db)
        {
            _engine = engine;
            _identity = identityProvider;
            _serviceConfigManager = serviceConfigManager;
            _processRunner = processRunner;

        }
        
        
        [HttpGet("process", Name = "LongInvocationAutoRestartWithPersistentLogging")]
        public async Task<IActionResult> Index()
        {
            _processRunner.Run(
                // Instructions
                new ProcessOptions()
                {
                    Executable = "cmd.exe",
                    Arguments = null,
                    Daemonized = true,
                    Monitor = true,
                    MonitoringInterval = 10,
                    DisposeOnExit = false,
                    InvokeAsSuperuser = true,
                    AttachLogToConsole = true,
                    WorkingDirectory = Program.GetAssemblyLocation()
                },
                // The calling object.
                null
            );

            return Ok(_response);
        }
    }
}