using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Extensions;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides GraphQL extensions to the <see cref="IEndpointConventionBuilder"/>.
    /// </summary>
    public static class HttpEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a GraphQL endpoint to the endpoint configurations.
        /// </summary>
        /// <param name="endpointRouteBuilder">
        /// The <see cref="IEndpointConventionBuilder"/>.
        /// </param>
        /// <param name="path">
        /// The path to which the GraphQL endpoint shall be mapped.
        /// </param>
        /// <param name="schemaName">
        /// The name of the schema that shall be used by this endpoint.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IEndpointConventionBuilder"/> so that
        /// configuration can be chained.
        /// </returns>
        public static GraphQLEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string path = "/graphql",
            NameString schemaName = default) =>
            MapGraphQL(endpointRouteBuilder, new PathString(path), schemaName);

        /// <summary>
        /// Adds a GraphQL endpoint to the endpoint configurations.
        /// </summary>
        /// <param name="endpointRouteBuilder">
        /// The <see cref="IEndpointConventionBuilder"/>.
        /// </param>
        /// <param name="path">
        /// The path to which the GraphQL endpoint shall be mapped.
        /// </param>
        /// <param name="schemaName">
        /// The name of the schema that shall be used by this endpoint.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IEndpointConventionBuilder"/> so that
        /// configuration can be chained.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="endpointRouteBuilder" /> is <c>null</c>.
        /// </exception>
        public static GraphQLEndpointConventionBuilder MapGraphQL(
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

            return new GraphQLEndpointConventionBuilder(
                endpointRouteBuilder
                    .Map(pattern, requestPipeline.Build())
                    .WithDisplayName("Hot Chocolate GraphQL Pipeline"));
        }

        /// <summary>
        /// Specifies the GraphQL server options.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="GraphQLEndpointConventionBuilder"/>.
        /// </param>
        /// <param name="serverOptions">
        /// The GraphQL server options.
        /// </param>
        /// <returns>
        /// Returns the <see cref="GraphQLEndpointConventionBuilder"/> so that
        /// configuration can be chained.
        /// </returns>
        public static GraphQLEndpointConventionBuilder WithOptions(
            this GraphQLEndpointConventionBuilder builder,
            GraphQLServerOptions serverOptions) =>
            builder.WithMetadata(serverOptions);

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
