using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
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
            IFileProvider fileProvider = CreateFileProvider();

            requestPipeline
                .UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault)
                .UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault)
                .UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault)
                .UseMiddleware<ToolDefaultFileMiddleware>(fileProvider, path)
                .UseMiddleware<ToolOptionsFileMiddleware>(schemaNameOrDefault, path)
                .UseMiddleware<ToolStaticFileMiddleware>(fileProvider, path)
                .UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault)
                .Use(next => context => 
                {
                    context.Response.StatusCode = 404;
                    return Task.CompletedTask;
                });

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

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(HttpEndpointRouteBuilderExtensions);
            string resourceNamespace = typeof(MiddlewareBase).Namespace + ".Resources";

            return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
        }
    }
}
