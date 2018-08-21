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
using System.IO;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Text;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Jobs
{
    /// <summary>
    /// Configuration class that will handle properties for the database backup job.
    /// </summary>
    public class BackupConfiguration
    {
        public int NumberToKeep { get; set; }
        public bool Enabled { get; set; }
        public string Frequency { get; set; }
    }

    public class DatabaseBackupJob : IJob
    {
        // Class Dependencies
        private readonly ILogger<DatabaseBackupJob> _logger;
        private readonly AppConfig _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>
        /// <param name="logger"></param>
        public DatabaseBackupJob(IOptionsMonitor<AppConfig> configMonitor, ILogger<DatabaseBackupJob> logger)
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
            try
            {
                var destination = Path.Combine(RootBackupDirectory, string.Format("db.{0}.sqlite", GetEpochTimestamp()));
                File.Copy(Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir, "db.sqlite"), destination);
                _logger.LogInformation("Backup has been written: {0}", destination);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "A problem occured while attempting to perform a backup of the database.");
                throw;
            }
        }

        /// <summary>
        /// Pointer to the root directory that backups should occur in.
        /// </summary>
        private string RootBackupDirectory => Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir, "Backups");

        /// <summary>
        /// Utility function to create the root backup directory.
        /// </summary>
        private void CreateRootBackupDirectory()
        {
            _logger.LogDebug($"The root backup directory ({RootBackupDirectory}) does not exist, creating...");
            Directory.CreateDirectory(RootBackupDirectory);
        }

        /// <summary>
        /// Utility function to check if the root backup directory exists.
        /// </summary>
        /// <returns></returns>
        private bool RootBackupDirectoryExists() => Directory.Exists(RootBackupDirectory);

        /// <summary>
        /// Get the unix timestamp.
        /// </summary>
        /// <returns></returns>
        public static string GetEpochTimestamp()
        {
            return DateTime.UtcNow.ToUnixTime().ToString();
        }
    }
}