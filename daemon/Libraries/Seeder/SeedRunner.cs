using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using ServiceStack.OrmLite;
using Spectero.daemon.Seeds;

namespace Spectero.daemon.Libraries.Seeder
{
    public class SeedRunner : ISeedRunner
    {
        private readonly IDbConnection _db;
        private readonly ILogger<SeedRunner> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        private readonly Type _seederType;
        
        public SeedRunner(IDbConnection db, ILogger<SeedRunner> logger, IServiceProvider serviceProvider)
        {
            _db = db;
            _logger = logger;
            _serviceProvider = serviceProvider;
            
            _seederType = typeof(BaseSeed);

            // This creates the tracker table if it doesn't exist, independent of the migrations framework.
            _db.CreateTableIfNotExists<Models.Seeder>();
        }

        public bool Run()
        {
            var implementers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => _seederType.IsAssignableFrom(p))
                .Where(p => p != _seederType) // Skip `BaseSeed` itself, it cannot be activated
                .ToArray();

            foreach (var seeder in implementers)
            {
                _logger.LogDebug($"Checking if {seeder} needs to be run.");
                
                var init = (ISeed) Activator.CreateInstance(seeder, _serviceProvider);

                var existingRunEntry = _db.Select<Models.Seeder>(x => x.Description == seeder.ToString()
                                                                      && x.Version == init.GetVersion()).FirstOrDefault();

                if (existingRunEntry != null)
                {
                    _logger.LogDebug($"Existing entry found for {seeder}, it was applied on {existingRunEntry.AppliedOn} with version {existingRunEntry.Version}");
                    continue;
                }

                _logger.LogDebug($"Run validated for {seeder}, attempting to call Up().");
                
                // Prob should wrap this in a try/catch
                init.Up();

                _logger.LogDebug($"Run finished for {seeder}, adding tracking entry into the DB.");
                _db.Insert(new Models.Seeder
                {
                    Description = seeder.ToString(),
                    Version = init.GetVersion(),
                    AppliedOn = DateTime.UtcNow
                });
            }

            return true;
        }
    }
}