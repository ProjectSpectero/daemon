using System.Data;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Seeder;

namespace Spectero.daemon.Seeds
{
    public class UserSeed : BaseSeed
    {
        private readonly IDbConnection _db;
        private readonly ILogger<SeedRunner> _logger;
        
        public UserSeed(IDbConnection db, ILogger<SeedRunner> logger)
        {
            _db = db;
            _logger = logger;
        }
        
        public override void Up()
        {
            throw new System.NotImplementedException();
        }

        public override void Down()
        {
            throw new System.NotImplementedException();
        }

        public override string GetVersion()
        {
            throw new System.NotImplementedException();
        }
    }
}