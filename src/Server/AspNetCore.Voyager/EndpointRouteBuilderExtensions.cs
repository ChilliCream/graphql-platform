#if NETCOREAPP3_0
using System;
using HotChocolate.AspNetCore.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HotChocolate.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapVoyager(this IEndpointRouteBuilder endpoints) =>
            MapVoyager(endpoints, new VoyagerOptions());

        public static IEndpointConventionBuilder MapVoyager(this IEndpointRouteBuilder endpoints, PathString queryPath) =>
            MapVoyager(endpoints, new VoyagerOptions
            {
                QueryPath = queryPath,
                Path = queryPath + new PathString("/voyager")
            });

        public static IEndpointConventionBuilder MapVoyager(this IEndpointRouteBuilder endpoints, PathString queryPath, PathString uiPath) =>
            MapVoyager(endpoints, new VoyagerOptions
            {
                QueryPath = queryPath,
                Path = uiPath
            });

        public static IEndpointConventionBuilder MapVoyager(this IEndpointRouteBuilder endpoints, VoyagerOptions options) =>
            MapVoyagerCore(
                endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
                options ?? throw new ArgumentNullException(nameof(options)));

        private static IEndpointConventionBuilder MapVoyagerCore(IEndpointRouteBuilder endpoints, VoyagerOptions options) =>
            endpoints
                .Map(options.Path,
                    endpoints
                        .CreateApplicationBuilder()
                        .UseVoyager(options)
                        .Build()
                )
                .WithDisplayName("GraphQL Voyager");
    }
}
#endif
