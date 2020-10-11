using System;
using HotChocolate;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string path = "/graphql",
            NameString schemaName = default) =>
            MapGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            PathString path,
            NameString schemaName = default)
        {
            if (endpointRouteBuilder is null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            path = path.ToString().TrimEnd('/');

            RoutePattern pattern = RoutePatternFactory.Parse(path + "/{**slug}");
            IApplicationBuilder requestPipeline = endpointRouteBuilder.CreateApplicationBuilder();
            NameString schemaNameOrDefault = schemaName.HasValue ? schemaName : Schema.DefaultName;

            requestPipeline.UseMiddleware<ToolDefaultFileMiddleware>(CreateFileProvider(), path);
            requestPipeline.UseMiddleware<ToolStaticFileMiddleware>(CreateFileProvider(), path);

            requestPipeline.UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault);
            requestPipeline.UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault);
            requestPipeline.UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault);
            requestPipeline.UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault);

            requestPipeline.UseBcpFileServer();

            return endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline");
        }

        [Obsolete("Use the new routing API.")]
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            PathString pathMatch = default,
            NameString schemaName = default)
        {
            if (applicationBuilder is null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            NameString schemaNameOrDefault = schemaName.HasValue ? schemaName : Schema.DefaultName;

            return applicationBuilder.Map(
                pathMatch,
                app =>
                {
                    app.UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault);
                });
        }

        public static IApplicationBuilder UseBcpFileServer(
            this IApplicationBuilder applicationBuilder)
        {
            var fileServerOptions = new FileServerOptions
            {
                FileProvider = CreateFileProvider(),
                EnableDefaultFiles = true,
                StaticFileOptions =
                {
                    ContentTypeProvider = new FileExtensionContentTypeProvider()
                }
            };

            return applicationBuilder.UseFileServer(fileServerOptions);
        }

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(HttpEndpointRouteBuilderExtensions);
            var resourceNamespace = "HotChocolate.AspNetCore.Resources";

            return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
        }
    }
}
