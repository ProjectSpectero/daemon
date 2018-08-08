/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using NLog.Web;
using RazorLight;
using RestSharp;
using ServiceStack.OrmLite;
using Spectero.daemon.HTTP.Filters;
using Spectero.daemon.Jobs;
using Spectero.daemon.Libraries.APM;
using Spectero.daemon.Libraries.CloudConnect;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.HTTP.Middlewares;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.LifetimeHandler;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Symlink;
using Spectero.daemon.Migrations;
using Spectero.daemon.Models;
using JobActivator = Spectero.daemon.Jobs.JobActivator;
using Utility = Spectero.daemon.Libraries.Core.Utility;

namespace Spectero.daemon
{
    public class Startup
    {
        private static readonly string CurrentDirectory = Program.GetAssemblyLocation();
        private IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            Directory.SetCurrentDirectory(CurrentDirectory);

            // Build the configuration.
            Configuration = BuildConfiguration(env.EnvironmentName);
        }

        // Dirty hack? Absolutely.
        // Avoids this bullshit though: https://github.com/aspnet/Hosting/issues/766
        public static IConfiguration BuildConfiguration(String envName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{envName}.json", true, true)
                .AddJsonFile("hosting.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Don't build a premature service provider from IServiceCollection, it only includes the services registered when the provider is built.
                
            // Root app config, this does not seem to work with complex JSON objects
            var appConfig = Configuration.GetSection("Daemon");
            services.Configure<AppConfig>(appConfig);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton(c =>
                InitializeDbConnection(appConfig["DatabaseDir"], SqliteDialect.Provider)
            );

            services.AddSingleton<IStatistician, Statistician>();
            
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
                    .ForFileSystem(Path.Combine(CurrentDirectory, appConfig["TemplateDirectory"]))
            );
            
            // HTTP Client for Job.
            services.AddSingleton<HttpClient, HttpClient>();

            // Symbolic Link Library
            services.AddSingleton<Symlink, Symlink>();

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

            services.AddSingleton<IJob, FetchCloudEngagementsJob>();
            
            services.AddSingleton<IJob, DatabaseBackupJob>();
            
            services.AddSingleton<IJob, UpdaterJob>();

            //services.AddScoped<IJob, TestJob>(); // This is mostly to test changes to the job activation infra.

            services.AddSingleton<Apm, Apm>();

            services.AddSingleton<IProcessRunner, ProcessRunner>();

            services.AddSingleton<ILifetimeHandler, LifetimeHandler>();

            services.AddScoped<EnforceLocalOnlyAccess>();

            services.AddSingleton<ICloudHandler, CloudHandler>();

            services.AddMvc();


            var builtProvider = services.BuildServiceProvider();
            services.AddHangfire(config =>
            {
                var connectionString = $"Data Source={appConfig["DatabaseDir"]}/jobs.sqlite;";
                
                config.UseSQLiteStorage(connectionString, new SQLiteStorageOptions());
                config.UseNLogLogProvider();
                // Please ENSURE that this is the VERY last call (to add services) in this method body. 
                // Provider once built is not retroactively updated from the collection.
                // If further dependencies are registered AFTER it is built, we'll get nothing.
                config.UseActivator(new JobActivator(builtProvider));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IOptionsMonitor<AppConfig> configMonitor, IApplicationBuilder app,
            IHostingEnvironment env, ILoggerFactory loggerFactory,
            IMigration migration, IAutoStarter autoStarter,
            IServiceProvider serviceProvider, IApplicationLifetime applicationLifetime,
            ILifetimeHandler lifetimeHandler)
        {
            // Create the filesystem marker that says Startup is now underway.
            // This is removed in LifetimeHandler once init finishes.
            // And yeah, the logging context is NOT yet available -_-
            if (! Utility.ManageStartupMarker())
                Console.WriteLine($"ERROR: The startup marker ({Utility.GetCurrentStartupMarker()}) could NOT be created.");
            
            var appConfig = configMonitor.CurrentValue;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("DefaultCORSPolicy");
                app.UseInterceptOptions(); // Return 200/OK with correct CORS to allow preflight requests, giant hack.
            }

            app.UseSpecteroErrorHandler();

            app.UseAddRequestIdHeader();

            // This HAS TO BE before the AddMVC call. Route registration fails otherwise.
            var option =
                new BackgroundJobServerOptions
                {
                    WorkerCount = 1
                }; // Limited by SQLite, can't deal with concurrency welp.
            app.UseHangfireServer(option);
            app.UseHangfireDashboard($"/jobs");

            app.UseMvc();

            // Initialize Nlog
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog(appConfig.LoggingConfig);
            app.AddNLogWeb();

            migration.Up();
            autoStarter.Startup();

            foreach (var implementer in serviceProvider.GetServices<IJob>())
            {
                if (!implementer.IsEnabled())
                    continue;

                // Magic, autowiring is magic.
                RecurringJob.AddOrUpdate(implementer.GetType().ToString(), () => implementer.Perform(),
                    implementer.GetSchedule);
            }

            applicationLifetime.ApplicationStarted.Register(lifetimeHandler.OnStarted);
            applicationLifetime.ApplicationStopping.Register(lifetimeHandler.OnStopping);
            applicationLifetime.ApplicationStopped.Register(lifetimeHandler.OnStopped);
        }

        /// <summary>
        /// Initialize a database connection using a provided connection string and provider.
        /// </summary>
        /// <param name="localResolvedFile"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private static IDbConnection InitializeDbConnection(string databaseDirectory, IOrmLiteDialectProvider provider)
        {
            // Reassign the database location to support the relative path of the assembly.
            var localResolvedFile = Path.Combine(CurrentDirectory, databaseDirectory, "db.sqlite");

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

            var factory = new OrmLiteConnectionFactory(localResolvedFile, provider);

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
                File.Copy(localResolvedFile, localResolvedFile + ".corrupt");

                // Create a new empty DB file for the schema to be initialized into
                // Dirty hack to ensure that the file's resource is actually released by the time ORMLite tries to open it
                using (var resource = File.Create(localResolvedFile))
                {
                    Console.WriteLine(
                        "Error Recovery: Executing automatic DB schema creation after saving the corrupt DB into db.sqlite.corrupt");
                }

                databaseContext = factory.Open();
            }

            return databaseContext;
        }
    }
}
