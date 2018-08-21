using System;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Spectero.daemon.Jobs;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Migration
{
    public class Migrator : IMigrator
    {
        private readonly AppConfig _config;
        private readonly IDbConnection _db;
        private readonly Type _modelType;

        /// <summary>
        /// Constructor
        /// Will inherit the database connector
        /// </summary>
        /// <param name="config"></param>
        /// <param name="db"></param>
        public Migrator(IOptionsMonitor<AppConfig> configMonitor, IDbConnection db)
        {
            _config = configMonitor.CurrentValue;
            _db = db;
            _modelType = typeof(BaseModel);
        }

        /// <summary>
        /// The function that will handle the migration and perform changes to the database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Migrate()
        {
            // Get versioning information and compare if we need to migrate.
            var schemaVersion = ConfigUtils.GetConfig(_db, ConfigKeys.SchemaVersion).Result;
            var daemonVersion = AppConfig.version;

            /*
             * TODO: Handle check implementation.
             * The check is whether the naked version for both the currently running instance and the schema version are different, and
             * if so, they need to be altered to conform to the currently running version's models.
             */

            // Get all defined models.
            var implementers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => _modelType.IsAssignableFrom(p))
                .Where(p => p != _modelType) // Skip `BaseModel` itself, it cannot be activated
                .ToArray();

            // Backup the current database.
            var currentDatabasePath = Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir, "db.sqlite");
            var backupDatabasePath = Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir, GenerateDatabaseBackupFilename());
            File.Copy(currentDatabasePath, backupDatabasePath);
            
            
            /*
             * Explanation:
             * We need to take a backup of the current db.sqlite before we can proceed further. Name it like <db.sqlite.version.timestamp>.
             * Reuse the jobs mechanism for it @Andrew, perhaps making a library of functions that are needed so they can be both used here and on the Backup job.
             * 
             * The ORM in use is https://github.com/ServiceStack/ServiceStack.OrmLite, read up on its docs first thoroughly.
             * 
             * Thirdly, explore one of these two ways:
             * 1. We load the DB contents into memory, drop the table, recreate it and push the values back.
             * Reflection will be required to map the values in our in-memory representation back to the model in SQLite.
             * This will require us to mess with and manually assign values. See Initialize.cs to learn how to drop/create tables.
             *
             * 2. We perform a DIFF of the DB Schema itself (not values), and then use ALTER TABLE statements
             * (assigning all new properties as nullable and using REAL/TEXT for numbers/strings).
             *
             * You can make use of OrmLiteConfigExtensions#GetModelDefinition(this Type modelType)
             * (an extension function on the Base `type` which your `implementers` (above) is an array of)
             * to skip reflecting yourself, and use the exact same implementation that ORMLite uses internally to discover fields.
             * _db has AlterColumn/AlterTable functions available.
             */


            throw new NotImplementedException();
        }

        /// <summary>
        /// Generate a fixated filename for the database backup we're about to make with an optional version.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public string GenerateDatabaseBackupFilename(string version = null)
        {
            // Get the default version if not specified.
            if (version == null) version = AppConfig.version;

            // Return the generated string.
            return string.Format("db.sqlite.{0}.{1}", version, DatabaseBackupJob.GetEpochTimestamp());
        }
    }
}