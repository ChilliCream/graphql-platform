using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.AspNetCore
{
    public static class HttpEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string pattern)
        {
            return MapGraphQL(
                endpointRouteBuilder,
                new QueryMiddlewareOptions { PatternString = pattern});
        }

        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            RoutePattern pattern)
        {
            return MapGraphQL(
                endpointRouteBuilder,
                new QueryMiddlewareOptions { Pattern = pattern});
        }

        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            QueryMiddlewareOptions options)
        {
            if (endpointRouteBuilder == null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            IApplicationBuilder applicationBuilder =
                endpointRouteBuilder.CreateApplicationBuilder();

            if (options.EnableHttpPost)
            {
                applicationBuilder.UseMiddleware<HttpPostMiddleware>();
            }

            if (options.EnableHttpGet)
            {
                applicationBuilder.UseMiddleware<HttpGetMiddleware>();
            }

            if (options.EnableHttpGetSdl)
            {
                applicationBuilder.UseMiddleware<HttpGetSchemaMiddleware>();
            }

            if (options.EnableSubscriptions)
            {
                applicationBuilder.UseMiddleware<SubscriptionMiddleware>();
            }

            return endpointRouteBuilder
                .Map(options.Pattern, applicationBuilder.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline");
        }
    }
}
