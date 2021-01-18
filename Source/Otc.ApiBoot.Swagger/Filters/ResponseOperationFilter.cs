using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.ApiBoot.Swagger
{
    public class ResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var requiredScopes = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy)
                .Distinct();

            if (requiredScopes.Any())
            {
                if (operation.Responses
                    .Any(a => a.Key == "401") == false)
                {
                    operation.Responses.Add("401",
                        new OpenApiResponse { Description = "Operação não autorizada." });
                }
            }
        }
    }
}
