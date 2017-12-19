using System;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
{
    [AllowAnonymous]
    public class SpaController : BaseController
    {
        private readonly IMemoryCache _cache;
        private readonly string _spaCacheKey;

        public SpaController(IOptionsSnapshot<AppConfig> appConfig, ILogger<SpaController> logger,
            IDbConnection db, IMemoryCache cache) : base(appConfig, logger, db)
        {
            _cache = cache;
            _spaCacheKey = "spa-fallback.contents";
        }

        public IActionResult Index()
        {
            Configuration viewBag;

            if (_cache.TryGetValue<Configuration>(_spaCacheKey, out var value))
                viewBag = value;
            else
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var webRootPath = AppConfig.WebRoot;
                var spaFileName = AppConfig.SpaFileName;

                var qualifiedPath = Path.Combine(currentDirectory, webRootPath, spaFileName);
                viewBag = new Configuration();

                try
                {
                    using (var streamReader = new StreamReader(qualifiedPath))
                    {
                        viewBag.Value = streamReader.ReadToEnd();
                        _cache.Set(_spaCacheKey, viewBag, AppConfig.SpaCacheTime > 0 ? TimeSpan.FromMinutes(AppConfig.SpaCacheTime) : TimeSpan.FromMinutes(5)); // Only cache it for 5 minutes at a time
                    }
                }
                catch (IOException e)
                {
                    Logger.LogError(e, "Could not serve SPA app: ");
                    return NotFound();
                }

            }

            return View(viewBag);
        }
    }
}