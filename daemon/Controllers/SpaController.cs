using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
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
        private readonly string spaCacheKey;

        public SpaController(IOptionsSnapshot<AppConfig> appConfig, ILogger<SpaController> logger,
            IDbConnection db, IMemoryCache cache) : base(appConfig, logger, db)
        {
            _cache = cache;
            spaCacheKey = "spa-fallback.contents";
        }

        public IActionResult Index()
        {
            Configuration viewBag;

            if (_cache.TryGetValue<Configuration>(spaCacheKey, out var value))
                viewBag = value;
            else
            {
                var currentDirectory = System.IO.Directory.GetCurrentDirectory();
                var webRootPath = AppConfig.WebRoot;
                var spaFileName = AppConfig.SpaFileName;

                var qualifiedPath = Path.Combine(currentDirectory, webRootPath, spaFileName);
                viewBag = new Configuration();

                try
                {
                    using (StreamReader streamReader = new StreamReader(qualifiedPath))
                    {
                        viewBag.Value = streamReader.ReadToEnd();
                        _cache.Set(spaCacheKey, viewBag, AppConfig.SpaCacheTime > 0 ? TimeSpan.FromMinutes(AppConfig.SpaCacheTime) : TimeSpan.FromMinutes(5)); // Only cache it for 5 minutes at a time
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logger.LogError(e, "Could not serve SPA app: ");
                }

            }

            return View(viewBag);
        }
    }
}