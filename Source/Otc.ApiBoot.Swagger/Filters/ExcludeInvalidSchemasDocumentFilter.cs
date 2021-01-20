using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Otc.DomainBase.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.ApiBoot.Swagger
{

    internal class ExcludeInvalidSchemasDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var types = Assembly.GetEntryAssembly()
                .GetTypes().ToList();

            //types.AddRange(new Type[] {
            //    typeof(InternalError),
            //    typeof(CoreError),
            //    typeof(CoreException<CoreError>),
            //    typeof(CoreException)});

            var domainBaseErros = new string[] {
                nameof(InternalError),
                nameof(CoreError),
                $"{nameof(CoreException)}[{nameof(CoreError)}]",
                nameof(CoreException)};

            var invalidSchemas = swaggerDoc.Components.Schemas
                    .Where(w =>
                        types.Any(a => a.Name == w.Key) == false &&
                        domainBaseErros.Any(a => a == w.Key) == false);

            foreach (KeyValuePair<string, OpenApiSchema> item in invalidSchemas)
            {
                swaggerDoc.Components.Schemas.Remove(item.Key);
            }
        }
    }
}
