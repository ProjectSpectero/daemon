using System;
using System.Data;
using System.IO;
using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using NLog.Web;
using RazorLight;
using RestSharp;
using ServiceStack.OrmLite;
using Spectero.daemon.Jobs;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.HTTP.Middlewares;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Migrations;
using Spectero.daemon.Models;
using JobActivator = Spectero.daemon.Jobs.JobActivator;

namespace Spectero.daemon
{
    public class Startup
    {
        private static readonly string CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
        private IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = BuildConfiguration(env.EnvironmentName);
        }

        // Dirty hack? Absolutely.
        // Avoids this bullshit though: https://github.com/aspnet/Hosting/issues/766
        public static IConfiguration BuildConfiguration(String envName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{envName}.json", true)
                .AddJsonFile("hosting.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }
 

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Don't build a premature service provider from IServiceCollection, it only includes the services registered when the provider is built.
            var appConfig = Configuration.GetSection("Daemon");

            services.Configure<AppConfig>(appConfig);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton(c =>
                InitializeDbConnection(appConfig["DatabaseFile"], SqliteDialect.Provider)
            );

            services.AddSingleton<IStatistician, Statistician>();

            services.AddSingleton<IAuthenticator, Authenticator>();

            services.AddSingleton<IIdentityProvider, IdentityProvider>();

            services.AddSingleton<ICryptoService, CryptoService>();

            services.AddSingleton<IMigration, Initialize>();

            services.AddSingleton<IServiceConfigManager, ServiceConfigManager>();

            services.AddSingleton<IServiceManager, ServiceManager>();

            services.AddSingleton<IOutgoingIPResolver, OutgoingIPResolver>();

            services.AddSingleton<IRazorLightEngine>(c =>
                new EngineFactory()
                    .ForFileSystem(System.IO.Path.Combine(CurrentDirectory, appConfig["TemplateDirectory"]))
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
                        IssuerSigningKey = services.BuildServiceProvider().GetService<ICryptoService>().GetJWTSigningKey()
                    };
                });


            services.AddCors(options =>
            {
                // TODO: Lock down this policy in production
                options.AddPolicy("DefaultCORSPolicy",
                    builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            services.AddSingleton<IAutoStarter, AutoStarter>();
            services.AddMemoryCache();
            services.AddSingleton<IRestClient>(c => new RestClient(AppConfig.ApiBaseUri));

            services.AddScoped<IJob, FetchCloudEngagementsJob>();

            //services.AddScoped<IJob, TestJob>(); // This is mostly to test changes to the job activation infra.
           
            services.AddMvc();


            var builtProvider = services.BuildServiceProvider();
            services.AddHangfire(config =>
            {
                config.UseSQLiteStorage(appConfig["JobsConnectionString"], new SQLiteStorageOptions());
                config.UseNLogLogProvider();
                // Please ENSURE that this is the VERY last call (to add services) in this method body. 
                // Provider once built is not retroactively updated from the collection.
                // If further dependencies are registered AFTER it is built, we'll get nothing.
                config.UseActivator(new JobActivator(builtProvider)); 
            });        
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IOptionsSnapshot<AppConfig> configMonitor, IApplicationBuilder app,
            IHostingEnvironment env, ILoggerFactory loggerFactory,
            IMigration migration, IAutoStarter autoStarter,
            IServiceProvider serviceProvider)
        {
            var appConfig = configMonitor.Value;
            var webRootPath = Path.Combine(CurrentDirectory, appConfig.WebRoot);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("DefaultCORSPolicy");
                app.UseInterceptOptions(); // Return 200/OK with correct CORS to allow preflight requests, giant hack.
            }

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(webRootPath)
            });

            app.UseAddRequestIdHeader();

            var option = new BackgroundJobServerOptions { WorkerCount = 1 }; // Limited by SQLite, can't deal with concurrency welp.

            app.UseHangfireServer(option);
            app.UseHangfireDashboard();

            app.UseMvc(routes =>
            {
                if (appConfig.SpaMode)
                {
                    routes.MapSpaFallbackRoute(
                        name: "spa-fallback",
                        defaults: new { controller = "Spa", action = "Index" }
                    );
                }
            });
            
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog(appConfig.LoggingConfig);
            app.AddNLogWeb();

            migration.Up();
            autoStarter.Startup();

            foreach (var implementer in serviceProvider.GetServices<IJob>())
            {
                if (! implementer.IsEnabled())
                    continue;

                // Magic, autowiring is magic.
                RecurringJob.AddOrUpdate(() => implementer.Perform(), implementer.GetSchedule);
            }
        }

        private static IDbConnection InitializeDbConnection(string connectionString, IOrmLiteDialectProvider provider)
        {
            // Validate that the DB connection can actually be used.
            // If not, attempt to fix it (for SQLite and corrupt files.)
            // Other providers not implemented (and are not possibly fixable for us anyway due to 3rd party daemons being involved)
            OrmLiteConfig.InsertFilter = (cmd, row) =>
            {
                if (row is BaseModel model)
                    model.CreatedDate = model.UpdatedDate = DateTime.UtcNow;
            };

            OrmLiteConfig.UpdateFilter = (cmd, row) =>
            {
                if (row is BaseModel model)
                    model.UpdatedDate = DateTime.UtcNow;
            };

            var factory = new OrmLiteConnectionFactory(connectionString, provider);
            
            IDbConnection databaseContext = null;

            try
            {
                databaseContext = factory.Open();
                databaseContext.TableExists<User>();
            }
            catch (SqliteException e)
            {
                // Message=SQLite Error 26: 'file is encrypted or is not a database'. most likely.
                // If we got here, our local database is corrupt.
                // Why Console.Writeline? Because the logging context is not initialized yet <_<'

                Console.WriteLine("Error: " + e.Message);
                databaseContext?.Close();

                // Move the corrupt DB file into db.sqlite.corrupt to aid recovery if needed.
                File.Copy(connectionString, connectionString + ".corrupt");

                // Create a new empty DB file for the schema to be initialized into
                // Dirty hack to ensure that the file's resource is actually released by the time ORMLite tries to open it
                using (var resource = File.Create(connectionString))
                {
                    Console.WriteLine("Error Recovery: Executing automatic DB schema creation after saving the corrupt DB into db.sqlite.corrupt");
                }

                databaseContext = factory.Open();
            }

            return databaseContext;

        }
    }
}