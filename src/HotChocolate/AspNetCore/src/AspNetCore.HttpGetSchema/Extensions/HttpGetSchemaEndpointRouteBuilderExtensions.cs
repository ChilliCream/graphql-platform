using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.AspNetCore
{
    public static class HttpGetSchemaEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGraphQLHttpGetSchema(
            this IEndpointRouteBuilder endpointRouteBuilder,
            RoutePattern pattern)
        {
            if (endpointRouteBuilder == null)
            {
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            }

            RequestDelegate pipeline = endpointRouteBuilder.CreateApplicationBuilder()
                .UseMiddleware<HttpGetSchemaMiddleware>()
                .Build();

            return endpointRouteBuilder.Map(pattern, pipeline);
        }
    }
}
