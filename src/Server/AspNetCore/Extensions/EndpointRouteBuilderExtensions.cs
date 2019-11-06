#if NETCOREAPP3_0
using System;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HotChocolate.AspNetCore
{
    public static class GraphQLEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphQL(this IEndpointRouteBuilder endpoints, PathString path) =>
            MapGraphQLCore(
                endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
                new QueryMiddlewareOptions
                {
                    Path = path.HasValue ? path : new PathString("/")
                });

        public static IEndpointConventionBuilder MapGraphQL(this IEndpointRouteBuilder endpoints, QueryMiddlewareOptions options) =>
            MapGraphQLCore(
                endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
                options ?? throw new ArgumentNullException(nameof(options)));

        private static IEndpointConventionBuilder MapGraphQLCore(IEndpointRouteBuilder endpoints, QueryMiddlewareOptions options) =>
            endpoints
                .Map(options.Path,
                    endpoints
                        .CreateApplicationBuilder()
                        .UseGraphQL(options)
                        .Build()
                )
                .WithDisplayName("GraphQL");
    }
}
#endif
