using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.AspNetCore.Extensions
{
    public static class HttpEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            string pattern = "/graphql",
            NameString schemaName = default) =>
            MapGraphQL(endpointRouteBuilder, RoutePatternFactory.Parse(pattern), schemaName);

        public static IEndpointConventionBuilder MapGraphQL(
            this IEndpointRouteBuilder endpointRouteBuilder,
            RoutePattern pattern,
            NameString schemaName = default)
        {
            if (endpointRouteBuilder == null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            IApplicationBuilder applicationBuilder =
                endpointRouteBuilder.CreateApplicationBuilder();
            
            applicationBuilder.UseMiddleware<WebSocketSubscriptionMiddleware>(
                schemaName.HasValue ? schemaName : Schema.DefaultName);
            applicationBuilder.UseMiddleware<HttpPostMiddleware>(
                schemaName.HasValue ? schemaName : Schema.DefaultName);

            return endpointRouteBuilder
                .Map(pattern, applicationBuilder.Build())
                .WithDisplayName("Hot Chocolate GraphQL Pipeline");
        }
    }
}
