using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Otc.DomainBase.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.ApiBoot.Swagger
{
    internal class ExcludeInvalidPropertiesSchemaFilter : ISchemaFilter
    {
        private static readonly IReadOnlyCollection<string> IgnoreProperties =
            typeof(Exception)
                .GetProperties()
                .Where(p => p.Name != nameof(Exception.Message))
                .Select(p => p.Name).ToArray();

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != null && schema.Properties != null && schema.Properties.Count > 0)
            {
                // Remove as propriedades do tipo base Exception, exceto Message
                if (typeof(Exception).IsAssignableFrom(context.Type))
                {
                    foreach (var ignoreProperty in IgnoreProperties)
                    {
                        var keyCheck = schema.Properties
                            .Keys.Where(k => k.ToLowerInvariant() == ignoreProperty.ToLowerInvariant());

                        if (keyCheck.Any() && keyCheck.Count() == 1)
                        {
                            schema.Properties.Remove(keyCheck.Single());
                        }
                    }
                }
                // Remove a propriedade Exception do tipo InternalError
                else if (typeof(InternalError).IsAssignableFrom(context.Type))
                {
                    var keyCheck = schema.Properties.Keys.Where(k =>
                        k.ToLowerInvariant() == nameof(InternalError.Exception).ToLowerInvariant());

                    if (keyCheck.Any() && keyCheck.Count() == 1)
                    {
                        schema.Properties.Remove(keyCheck.Single());
                    }
                }
            }
        }
    }
}
