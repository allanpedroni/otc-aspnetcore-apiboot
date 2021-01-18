using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Otc.AuthorizationContext.AspNetCore.Jwt;
using Otc.Caching.DistributedCache.All;

namespace Otc.AspNetCore.ApiBoot.TestHost
{
    public class HttpFixture<TStartup>
        where TStartup : ApiBootStartup
    {
        public TestServer CreateServer(Action<IServiceCollection> serviceConfiguration)
        {
            var builder = WebHost.CreateDefaultBuilder()
                .UseSetting(nameof(JwtConfiguration.Audience),
                    StaticConfiguration.jwtConfiguration.Audience)
                .UseSetting(nameof(JwtConfiguration.Issuer),
                    StaticConfiguration.jwtConfiguration.Issuer)
                .UseSetting(nameof(JwtConfiguration.SecretKey),
                    StaticConfiguration.jwtConfiguration.SecretKey)
                .UseSetting(nameof(ApiBootOptions.EnableSwagger), "False")
                .UseSetting(nameof(ApiBootOptions.EnableLogging), "False")
                .UseSetting(nameof(DistributedCacheConfiguration.CacheStorageType),
                    StorageType.Memory.ToString())
                .ConfigureServices(serviceConfiguration)
                .UseStartup<TStartup>();

            return new TestServer(builder);
        }
    }
}
