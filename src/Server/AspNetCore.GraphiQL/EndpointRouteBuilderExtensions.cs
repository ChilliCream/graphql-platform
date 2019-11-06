#if NETCOREAPP3_0
using System;
using HotChocolate.AspNetCore.GraphiQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HotChocolate.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphiQL(this IEndpointRouteBuilder endpoints) =>
            MapGraphiQL(endpoints, new GraphiQLOptions());

        public static IEndpointConventionBuilder MapGraphiQL(this IEndpointRouteBuilder endpoints, PathString queryPath) =>
            MapGraphiQL(endpoints, new GraphiQLOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/graphiql")
            });

        public static IEndpointConventionBuilder MapGraphiQL(this IEndpointRouteBuilder endpoints, PathString queryPath, PathString uiPath) =>
            MapGraphiQL(endpoints, new GraphiQLOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });

        public static IEndpointConventionBuilder MapGraphiQL(this IEndpointRouteBuilder endpoints, GraphiQLOptions options) =>
            MapGraphiQLCore(
                endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
                options ?? throw new ArgumentNullException(nameof(options)));

        private static IEndpointConventionBuilder MapGraphiQLCore(IEndpointRouteBuilder endpoints, GraphiQLOptions options) =>
            endpoints
                .Map(options.Path,
                    endpoints
                        .CreateApplicationBuilder()
                        .UseGraphiQL(options)
                        .Build()
                )
                .WithDisplayName("GraphQL GraphiQL");
    }
}
#endif
