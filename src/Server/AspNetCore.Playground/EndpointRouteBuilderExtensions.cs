#if NETCOREAPP3_0
using System;
using HotChocolate.AspNetCore.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HotChocolate.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapPlayground(this IEndpointRouteBuilder endpoints) =>
            MapPlayground(endpoints, new PlaygroundOptions());

        public static IEndpointConventionBuilder MapPlayground(this IEndpointRouteBuilder endpoints, PathString queryPath) =>
            MapPlayground(endpoints, new PlaygroundOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/playground")
            });

        public static IEndpointConventionBuilder MapPlayground(this IEndpointRouteBuilder endpoints, PathString queryPath, PathString uiPath) =>
            MapPlayground(endpoints, new PlaygroundOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });

        public static IEndpointConventionBuilder MapPlayground(this IEndpointRouteBuilder endpoints, PlaygroundOptions options) =>
            MapPlaygroundCore(
                endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
                options ?? throw new ArgumentNullException(nameof(options)));

        private static IEndpointConventionBuilder MapPlaygroundCore(IEndpointRouteBuilder endpoints, PlaygroundOptions options) =>
            endpoints
                .Map(options.Path,
                    endpoints
                        .CreateApplicationBuilder()
                        .UsePlayground(options)
                        .Build()
                )
                .WithDisplayName("GraphQL Playground");
    }
}
#endif
