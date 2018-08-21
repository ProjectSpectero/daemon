using System;
using System.Data;
using System.Linq;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Migration
{
    public class Migrator : IMigrator
    {
        private readonly IDbConnection _db;
        private readonly Type _modelType;
        
        public Migrator(IDbConnection db)
        {
            _db = db;
            _modelType = typeof(BaseModel);
        }

        public bool Migrate()
        {
            var schemaVersion = ConfigUtils.GetConfig(_db, ConfigKeys.SchemaVersion).Result;

            var daemonVersion = AppConfig.version;
            
            // The check is whether the naked version for both the currently running instance and the schema version are different, and if so, they need to be altered to conform to the currently running version's models.
            
            // First, we have to find all the defined models. Let's do that.
            
            var implementers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => _modelType.IsAssignableFrom(p))
                .Where(p => p != _modelType) // Skip `BaseModel` itself, it cannot be activated
                .ToArray();
            
            
            // Secondly, we need to take a backup of the current db.sqlite before we can proceed further. Name it like <db.sqlite.version.timestamp>.
            // Reuse the jobs mechanism for it @Andrew, perhaps making a library of functions that are needed so they can be both used here and on the Backup job.
            
            // The ORM in use is https://github.com/ServiceStack/ServiceStack.OrmLite, read up on its docs first thoroughly.

            // Thirdly, explore one of these two ways.
            // 1. We load the DB contents into memory, drop the table, recreate it and push the values back. Reflection will be required to map the values in our in-memory representation back to the model in SQLite.
            //     This will require us to mess with and manually assign values. See Initialize.cs to learn how to drop/create tables.
            
            // 2. We perform a DIFF of the DB Schema itself (not values), and then use ALTER TABLE statements (assigning all new properties as nullable and using REAL/TEXT for numbers/strings).
            
            // You can make use of OrmLiteConfigExtensions#GetModelDefinition(this Type modelType) (an extension function on the Base `type` which your `implementers` (above) is an array of) to skip reflecting yourself, and use the exact same implementation that ORMLite uses internally to discover fields.
            // _db has AlterColumn/AlterTable functions available.
            
            throw new NotImplementedException();
        }
    }
}