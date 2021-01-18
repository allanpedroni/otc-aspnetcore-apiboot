using Microsoft.Extensions.Options;
using Otc.ApiBoot.Swagger;
using Otc.ApiBoot.Swagger.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcApiBootSwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddOtcApiBootSwagger(
            this IServiceCollection services, SwaggerApiInfo swaggerApiInfo)
        {
            if (services == null)
            {
                throw new System.ArgumentNullException(nameof(services));
            }

            if (swaggerApiInfo is null)
            {
                throw new System.ArgumentNullException(nameof(swaggerApiInfo));
            }

            services.AddSingleton(swaggerApiInfo);

            services
                .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen();

            return services;
        }
    }
}
