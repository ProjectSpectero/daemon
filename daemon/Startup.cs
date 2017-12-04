using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using NLog.Web;
using RazorLight;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.HTTP.Middlewares;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Migrations;

namespace Spectero.daemon
{
    public class Startup
    {
        private string _currentDirectory = System.IO.Directory.GetCurrentDirectory();

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddJsonFile("hosting.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var appConfig = Configuration.GetSection("Daemon");
            services.Configure<AppConfig>(appConfig);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton(c =>
                new OrmLiteConnectionFactory(appConfig["DatabaseFile"], SqliteDialect.Provider).Open()
            );

            services.AddSingleton<IStatistician, Statistician>();

            services.AddSingleton<IAuthenticator, Authenticator>();

            services.AddSingleton<IIdentityProvider, IdentityProvider>();

            services.AddSingleton<ICryptoService, CryptoService>();

            services.AddSingleton<IMigration, Initialize>();

            services.AddSingleton<IServiceConfigManager, ServiceConfigManager>();

            services.AddSingleton<IServiceManager, ServiceManager>();

            services.AddSingleton<IRazorLightEngine>(c =>
                new EngineFactory()
                    .ForFileSystem(System.IO.Path.Combine(_currentDirectory, appConfig["TemplateDirectory"]))
            );

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            services.BuildServiceProvider().GetService<ICryptoService>().GetJWTSigningKey()
                    };
                });

            services.AddMvc();
            services.AddMemoryCache();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, IMigration migration)
        {
            var appConfig = Configuration.GetSection("Daemon");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseAddCORS();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), appConfig["WebRoot"]))
            });

            app.UseAddRequestIdHeader();
            app.UseMvc();
            
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog("nlog.config");
            app.AddNLogWeb();

            migration.Up();
        }
    }
}