using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.AspNetCore
{
    public static class HttpGetApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder) =>
            UseGraphQLHttpGet(applicationBuilder, new PathString());

        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder,
            PathString path)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            return applicationBuilder.Map(
                path,
                b => b.UseMiddleware<HttpGetMiddleware>());
        }
    }
}
