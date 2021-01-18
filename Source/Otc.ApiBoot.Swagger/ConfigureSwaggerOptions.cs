using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Otc.ApiBoot.Swagger.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Otc.ApiBoot.Swagger
{

    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider provider;
        private readonly SwaggerApiInfo swaggerApiInfo;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        /// <param name="apiMetadata">The api metadata object who describe all metadata about api</param>
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider,
            SwaggerApiInfo swaggerApiInfo, ILoggerFactory loggerFactory)
        {
            this.provider = provider;
            this.swaggerApiInfo = swaggerApiInfo ??
                throw new ArgumentNullException(nameof(swaggerApiInfo));
            logger = loggerFactory?.CreateLogger<ConfigureSwaggerOptions>();
        }

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            options.AddSecurityDefinition("Bearer",
                new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Name = "Authorization",
                            Type = SecuritySchemeType.ApiKey,
                            In = ParameterLocation.Header,
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                         },
                         new string[] {}
                     }
                });

            //if (swaggerApiInfo.EnableStringEnumConverter)
            //{
            //    //SE NÂO TIVER SIDO ATIVADO EM AddJsonOptions deve-se ativar esse.
            //    options.DescribeAllEnumsAsStrings();
            //}

            options.CustomSchemaIds(type => GetCustomSchemaIdsForGenericTypes(type));

            // add a custom operation filter which sets default values
            options.OperationFilter<DefaultValuesOperationFilter>();

            //Remover parametro api-version
            options.OperationFilter<RemoveQueryApiVersionOperationFilter>();

            // Filtro referente ao mecanismo de tratamento de excecoes (Otc.ExceptionHandling):
            // Remove diversas propriedades do tipo Exception do schema do swagger
            options.SchemaFilter<ExcludeInvalidPropertiesSchemaFilter>();

            //Filtro para remover objetos que não fazem parte do contexto
            // Remove diversas propriedades do schema do swagger
            options.DocumentFilter<ExcludeInvalidSchemasDocumentFilter>();

            //Filtro para incluir a descrição do enum a descrição do mesmo
            options.DocumentFilter<DisplayEnumsWithValuesDocumentFilter>();
            //options.SchemaFilter<EnumSchemaFilter>();

            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }

            if (File.Exists(swaggerApiInfo.XmlCommentsFilePath))
            {
                // integrate xml comments
                options.IncludeXmlComments(swaggerApiInfo.XmlCommentsFilePath);
            }
            else
            {
                logger.LogWarning("Could not read Xml comments file, path '{XmlCommentsFilePath}' " +
                    "not exists.", swaggerApiInfo.XmlCommentsFilePath);
            }


            OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
            {
                var info = new OpenApiInfo()
                {
                    Title = $"{swaggerApiInfo.ApiName} (Build {swaggerApiInfo.BuildId})",
                    Version = description.ApiVersion.ToString(),
                    Description = swaggerApiInfo.ApiDescription
                };

                if (description.IsDeprecated)
                {
                    info.Description += " This API version has been deprecated.";
                }

                return info;
            }

            static string GetCustomSchemaIdsForGenericTypes(Type type)
            {
                var customSchemaId = type.Name;

                try
                {
                    if (type.IsConstructedGenericType && type.IsGenericType)
                    {
                        customSchemaId = type.BaseType.Name;

                        var genericType = type.GenericTypeArguments.FirstOrDefault();

                        if (genericType != null)
                        {
                            customSchemaId =
                                string.Concat(customSchemaId, $"[{genericType.Name}]");
                        }
                    }
                }
                catch (Exception) { } //get the default name of type if some error happens

                return customSchemaId;
            }
        }
    }
}
