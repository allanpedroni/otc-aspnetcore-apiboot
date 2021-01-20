using Otc.AuthorizationContext.AspNetCore.Jwt;

namespace Otc.ApiBoot.TestHost
{
    internal static class StaticConfiguration
    {
        internal static readonly JwtConfiguration JwtConfiguration = new JwtConfiguration()
        {
            Audience = "ole tecnologia",
            Issuer = "ole tecnologia",
            SecretKey = "abcdefghijklmnopqrs"
        };
    }
}
