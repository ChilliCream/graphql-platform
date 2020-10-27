using System;
using HotChocolate;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IGraphQLEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string path = "/graphql",
            NameString schemaName = default) =>
                MapGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

        public static IGraphQLEndpointConventionBuilder MapGraphQL(
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
            IFileProvider fileProvider = CreateFileProvider();

            requestPipeline
                .UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault)
                .UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault)
                .UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault)
                .UseMiddleware<ToolDefaultFileMiddleware>(fileProvider, path)
                .UseMiddleware<ToolOptionsFileMiddleware>(schemaNameOrDefault, path)
                .UseMiddleware<ToolStaticFileMiddleware>(fileProvider, path)
                .UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault);

            IEndpointConventionBuilder endpointConventionBuilder = endpointRouteBuilder
                .Map(pattern, requestPipeline.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline");

            return new GraphQLEndpointConventionBuilder(endpointConventionBuilder);
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

        private static IFileProvider CreateFileProvider()
        {
            var type = typeof(EndpointRouteBuilderExtensions);
            var resourceNamespace = typeof(MiddlewareBase).Namespace + ".Resources";

            return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
        }
    }
}
