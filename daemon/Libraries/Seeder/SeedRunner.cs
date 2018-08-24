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
        
        private readonly Type _seederType;
        
        public SeedRunner(IDbConnection db, ILogger<SeedRunner> logger)
        {
            _db = db;
            _logger = logger;
            _seederType = typeof(BaseSeed);
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
                
                var init = (ISeed) Activator.CreateInstance(seeder, _db, _logger);

                var existingRunEntry = _db.Select<Models.Seeder>(x => x.Description == seeder.ToString()
                                                                      && x.Version == init.GetVersion()).FirstOrDefault();

                if (existingRunEntry != null)
                {
                    _logger.LogDebug($"Existing entry found for {seeder}, it was applied on {existingRunEntry.AppliedOn} with version {existingRunEntry.Version}");
                    continue;;
                }

                _logger.LogDebug($"Run validated, attempting to call Up() on {seeder}");
                
                // Prob should wrap this in a try/catch
                init.Up();
            }

            return true;
        }
    }
}