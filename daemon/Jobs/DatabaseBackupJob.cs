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
    /// <summary>
    /// Configuration class that will handle properties for the database backup job.
    /// </summary>
    public class BackupConfiguration
    {
        public int NumberToKeep;
        public bool Enabled;
        public string Frequency;
    }

    public class DatabaseBackupJob : IJob
    {
        // Class Dependencies
        private readonly ILogger<FetchCloudEngagementsJob> _logger;
        private readonly AppConfig _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>
        /// <param name="logger"></param>
        public DatabaseBackupJob(IOptionsMonitor<AppConfig> configMonitor, ILogger<FetchCloudEngagementsJob> logger)
        {
            _logger = logger;
            _config = configMonitor.CurrentValue;

            logger.LogDebug("DBJ: init successful, dependencies processed.");
        }

        /// <summary>
        /// The cron time that the schedule should run.
        /// </summary>
        /// <returns></returns>
        public string GetSchedule() => _config.Backups.Frequency;

        /// <summary>
        /// Determine if this job is enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled() => _config.Backups.Enabled;


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
                _logger.LogError("DBJ: Job enabled, but matching criterion does not match. This should not happen, silently going back to sleep.");
                return;
            }

            // Validate that the backup folders exist.
            if (!RootBackupDirectoryExists()) CreateRootBackupDirectory();

            // Save the database into the dated directory.
            var destination = Path.Combine(RootBackupDirectory, string.Format("db.{0}.sqlite", DateTime.UtcNow));
            _logger.LogInformation("Starting database backup.");
            File.Copy(Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir, "db.sqlite"), destination);
            _logger.LogInformation("Database backup successful.\nDatabase has been saved to {0}", destination);
        }

        /// <summary>
        /// Pointer to the root directory that backups should occur in.
        /// In the event that we write something else inside the daemon that needs easy access to find the path, we can call this static function.
        /// </summary>
        public static string RootBackupDirectory => Path.Combine(Program.GetAssemblyLocation(), "Database", "Backups");

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