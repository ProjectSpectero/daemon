using System;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Seeds
{
    public abstract class BaseSeed : ISeed
    {
        protected readonly IDbConnection _db;
        protected readonly AppConfig _config;
        
        // Not readonly, an override is expected in child classes.
        protected ILogger<BaseSeed> _logger;
        
        public BaseSeed(IServiceProvider serviceProvider)
        {
            _db = serviceProvider.GetRequiredService<IDbConnection>();
            _logger = serviceProvider.GetRequiredService<ILogger<BaseSeed>>();
            _config = serviceProvider.GetRequiredService<IOptionsMonitor<AppConfig>>().CurrentValue;
        }
        
        public abstract void Up();
        public abstract void Down();
        public abstract string GetVersion();
    }
}