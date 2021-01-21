using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.ApiBoot.Swagger
{
    internal class DisplayEnumsWithValuesDocumentFilter : IDocumentFilter
    {
        /// <summary>
        /// Apply the filter.
        /// </summary>
        /// <param name="swaggerDoc"><see cref="OpenApiDocument"/>.</param>
        /// <param name="context"><see cref="DocumentFilterContext"/>.</param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var schemaDictionaryItem in swaggerDoc.Components.Schemas)
            {
                var schema = schemaDictionaryItem.Value;
                var description = AddEnumValuesDescription(schema, schemaDictionaryItem.Key);
                if (description != null)
                {
                    if (schema.Description == null)
                    {
                        schema.Description = description;
                    }
                    else if (!schema.Description.Contains(description))
                    {
                        schema.Description += description;
                    }
                }
            }

            if (swaggerDoc.Paths.Count <= 0)
            {
                return;
            }

            // add enum descriptions to input parameters of every operation
            foreach (var parameter in
                swaggerDoc.Paths.Values
                    .SelectMany(v => v.Operations)
                    .SelectMany(op => op.Value.Parameters))
            {
                if (parameter.Schema.Reference == null)
                {
                    continue;
                }

                var componentReference = parameter.Schema.Reference.Id;
                var schema = swaggerDoc.Components.Schemas[componentReference];

                var description = AddEnumValuesDescription(schema, componentReference);
                if (description != null)
                {
                    if (parameter.Description == null)
                    {
                        parameter.Description = description;
                    }
                    else if (!parameter.Description.Contains(description))
                    {
                        parameter.Description += description;
                    }
                }
            }

            // add enum descriptions to request body
            foreach (var operation in swaggerDoc.Paths.Values.SelectMany(v => v.Operations))
            {
                var requestBodyContents = operation.Value.RequestBody?.Content;
                if (requestBodyContents != null)
                {
                    foreach (var requestBodyContent in requestBodyContents)
                    {
                        if (requestBodyContent.Value.Schema?.Reference?.Id != null)
                        {
                            var schema =
                                context.SchemaRepository.Schemas[requestBodyContent.Value.Schema?.Reference?.Id];
                            if (schema != null)
                            {
                                requestBodyContent.Value.Schema.Description = schema.Description;
                                requestBodyContent.Value.Schema.Extensions = schema.Extensions;
                            }
                        }
                    }
                }
            }
        }

        private Type GetEnumTypeByName(string enumTypeName)
        {
            return Assembly.GetEntryAssembly()
                    .GetTypes()
                    .FirstOrDefault(s => s.Name == enumTypeName);
        }

        private string AddEnumValuesDescription(OpenApiSchema schema, string key)
        {
            if (schema.Enum == null || schema.Enum.Count == 0)
            {
                return null;
            }

            var enumType = GetEnumTypeByName(key);

            var quebraLinha = $"{Environment.NewLine}{Environment.NewLine}";

            var sb = new StringBuilder();
            for (var i = 0; i < schema.Enum.Count; i++)
            {
                if (schema.Enum[i] is OpenApiString schemaEnumString)
                {
                    Enum.TryParse(enumType, schemaEnumString.Value, out object value);

                    if (value != null)
                    {
                        sb.Append($"{quebraLinha}{Convert.ToInt32(value)} = {schemaEnumString.Value}");
                    }
                    else
                    {
                        sb.Append($"{quebraLinha}{quebraLinha}{schemaEnumString.Value}");
                    }
                }
            }
            return sb.ToString();
        }
    }
}