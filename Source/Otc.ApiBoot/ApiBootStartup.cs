using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Graceterm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otc.ApiBoot.BuildTracker;
using Otc.ApiBoot.Configuration;
using Otc.ApiBoot.Controllers;
using Otc.ApiBoot.Swagger.Configuration;
using Otc.AuthorizationContext.AspNetCore.Jwt;
using Otc.Caching.DistributedCache.All;
using Otc.Extensions.Configuration;
using Otc.Mvc.Filters;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

namespace Otc.ApiBoot
{
    public abstract class ApiBootStartup
    {
        protected ApiBootStartup(IConfiguration configuration)
        {
            Configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            ApiBootOptions = Configuration.SafeGet<ApiBootOptions>();
        }

        public IConfiguration Configuration { get; }
        public ApiBootOptions ApiBootOptions { get; }
        protected abstract ApiMetadata ApiMetadata { get; }

        private static readonly string BuildIdFilePath = Path.Combine(AppContext.BaseDirectory,
                "buildid");
        private string XmlCommentsFilePath
        {
            get
            {
                var fileName = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                return Path.Combine(AppContext.BaseDirectory, fileName);
            }
        }

        private string BuildId
        {
            get
            {
                if (File.Exists(BuildIdFilePath))
                {
                    var content = File.ReadAllText(BuildIdFilePath);

                    return content?.Trim();
                }

                return "n/a";
            }
        }

        protected abstract void ConfigureApiServices(IServiceCollection services);

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options => options.Filters.Add<ExceptionFilter>())
                .AddMvcOptions(options =>
                    ConfigureMvcOptions(options))
                .AddJsonOptions(options =>
                    ConfigureJsonOptions(options));

            services.AddHttpContextAccessor();

            services.AddHttpClientWithCorrelation();

            services.AddLogging(config => ConfigureLoggingSettings(config));

            services.AddOtcAspNetCoreJwtAuthorizationContext(
                Configuration.SafeGet<JwtConfiguration>());

            ConfigureApiVersioning(services);

            ConfigureSwagger(services);

            //UseExceptionHandler Middleware - Discutir.
            //https://github.com/dotnet/AspNetCore.Docs/blob/master/aspnetcore/fundamentals/error-handling/samples/5.x/ErrorHandlingSample/StartupLambda.cs
            //https://jasonwatmore.com/post/2020/10/02/aspnet-core-31-global-error-handler-tutorial

            services.AddExceptionHandling();

            services.AddOtcDistributedCache(Configuration.SafeGet<DistributedCacheConfiguration>());

            ConfigureApiServices(services);
        }

        public void Configure(IApplicationBuilder app,
            IApiVersionDescriptionProvider apiVersionDescriptionProvider)
        {
            app.UseGraceterm(options =>
            {
                options.IgnorePath(HealthChecksController.RoutePath);
            });

            app.UseApiVersioning();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseBuildIdTracker(BuildId);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //TO REVIEW
            //https://www.c-sharpcorner.com/article/maintaine-multiple-versions-of-api-code-base-using/
            //https://stackoverflow.com/questions/40929916/how-to-set-up-swashbuckle-vs-microsoft-aspnetcore-mvc-versioning
            //https://www.google.com/search?safe=off&sxsrf=ALeKk00tyN_qlbRzJyEKreYim4CwCn-0_w%3A1610396175300&ei=D7L8X-PXEfqz5OUPobWWsAY&q=Microsoft.AspNetCore.Mvc.Versioning+.net+5&oq=Microsoft.AspNetCore.Mvc.Versioning+.net+5&gs_lcp=CgZwc3ktYWIQAzoFCAAQyQM6CAgAEBYQChAeUOesEVjnrBFgr74RaABwAHgAgAH3AogBiAWSAQUyLTEuMZgBAKABAaoBB2d3cy13aXrAAQE&sclient=psy-ab&ved=0ahUKEwijm-ef2ZTuAhX6GbkGHaGaBWYQ4dUDCA0&uact=5

            if (ApiBootOptions.EnableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(
                    options =>
                    {
                        // build a swagger endpoint for each discovered API version
                        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                        {
                            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                description.GroupName.ToUpperInvariant());
                        }
                    });
            }
        }

        public virtual void ConfigureMvcOptions(MvcOptions options) { }

        public virtual void ConfigureJsonOptions(JsonOptions options)
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.IgnoreNullValues = true;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.AllowTrailingCommas = false;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;

            if (ApiBootOptions.EnableStringEnumConverter)
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        }

        private void ConfigureLoggingSettings(ILoggingBuilder configure)
        {
            //ANALISAR MUDANÇAS
            //https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/obsolete-consoleloggeroptions-properties

            configure.ClearProviders();

            if (ApiBootOptions.EnableLogging)
            {
                var loggerConfiguration = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .Enrich.WithExceptionDetails();

                if (ApiBootOptions.LoggingType != LoggingType.SerilogRawConfiguration)
                {
                    loggerConfiguration = loggerConfiguration
                        .Enrich.FromLogContext()
                        .Enrich.WithProcessId()
                        .Enrich.WithProcessName()
                        .Enrich.WithThreadId()
                        .Enrich.WithEnvironmentUserName()
                        .Enrich.WithMachineName();

                    if (ApiBootOptions.LoggingType == LoggingType.ApiBootFile)
                    {
                        loggerConfiguration = loggerConfiguration.WriteTo
                            .Async(a => a.File($"logs/log-.txt", rollingInterval: RollingInterval.Day));
                    }
                    else
                    {
                        loggerConfiguration = loggerConfiguration.WriteTo
                            .Async(a => a.Console(new JsonFormatter()));
                    }
                }

                Log.Logger = loggerConfiguration.CreateLogger();

                configure.AddSerilog();
                configure.AddDebug();
            }
        }

        private void ConfigureApiVersioning(IServiceCollection services)
        {
            services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = ApiVersion.Parse(ApiMetadata.DefaultApiVersion);
            });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            if (ApiBootOptions.EnableSwagger)
            {
                var swaggerApiInfo = new SwaggerApiInfo()
                {
                    ApiDescription = ApiMetadata.Description,
                    ApiName = ApiMetadata.Name,
                    BuildId = BuildId,
                    XmlCommentsFilePath = XmlCommentsFilePath
                };

                services.AddOtcApiBootSwagger(swaggerApiInfo);
            }
        }
    }
}