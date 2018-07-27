using System;
using System.Data;
using System.IO;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Identity;

namespace Spectero.daemon.Jobs
{
    public class DatabaseBackupJob : IJob
    {
        // Class Dependencies
        private readonly IRestClient _restClient;
        private readonly IDbConnection _db;
        private readonly IIdentityProvider _identityProvider;
        private readonly ILogger<FetchCloudEngagementsJob> _logger;
        private readonly IMemoryCache _cache;
        private readonly AppConfig _config;
        private readonly ICloudHandler _cloudHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="db"></param>
        /// <param name="restClient"></param>
        /// <param name="identityProvider"></param>
        /// <param name="logger"></param>
        /// <param name="cache"></param>
        /// <param name="configMonitor"></param>
        /// <param name="cloudHandler"></param>
        public DatabaseBackupJob(IDbConnection db, IRestClient restClient,
            IIdentityProvider identityProvider, ILogger<FetchCloudEngagementsJob> logger,
            IMemoryCache cache, IOptionsMonitor<AppConfig> configMonitor,
            ICloudHandler cloudHandler)
        {
            _restClient = restClient;
            _db = db;
            _identityProvider = identityProvider;
            _logger = logger;
            _cache = cache;
            _config = configMonitor.CurrentValue;
            _cloudHandler = cloudHandler;

            logger.LogDebug("FCEJ: init successful, dependencies processed.");
        }

        /// <summary>
        /// The cron time that the schedule should run.
        /// Currently configured for every night at midnight.
        /// </summary>
        /// <returns></returns>
        public string GetSchedule() => "0 0 * * *";

        /// <summary>
        /// Determine if this job is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled() => _cloudHandler.IsConnected().Result;


        /// <summary>
        /// The actual job function that will be executed.
        /// </summary>
        /// <exception cref="???"></exception>
        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Perform()
        {
            // Check to make sure this job is enabled.
            if (!IsEnabled())
            {
                _logger.LogError("FCEJ: Job enabled, but matching criterion does not match. This should not happen, silently going back to sleep.");
                return;
            }

            // Validate that the backup folders exist.
            if (!RootBackupDirectoryExists()) CreateRootBackupDirectory();
         
            // Save the database into the dated directory.
            var destination = Path.Combine(RootBackupDirectory, string.Format("db.{0}.sqlite", DateTime.UtcNow));
            _logger.LogInformation("Beginning to make daily backup of database...");
            File.Copy(_config.DatabaseFile, destination);
            _logger.LogInformation("Database backup successful.\nDatabase has been saved to {0}", destination);
        }

        /// <summary>
        /// Pointer to the root directory that backups should occur in.
        /// In the event that we write something else inside the daemon that needs easy access to find the path, we can call this static function.
        /// </summary>
        public static string RootBackupDirectory => Path.Combine(
            Path.Combine(Program.GetAssemblyLocation(), "Database"),
            "Backups"
        );

        /// <summary>
        /// Utility function to create the root backup directory.
        /// </summary>
        private void CreateRootBackupDirectory() => Directory.CreateDirectory(RootBackupDirectory);

        /// <summary>
        /// Utility function to check if the root backup directory exists.
        /// </summary>
        /// <returns></returns>
        private bool RootBackupDirectoryExists() => Directory.Exists(RootBackupDirectory);
    }
}