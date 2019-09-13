using System;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore
{
    public static class HttpGetApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder) =>
            UseGraphQLHttpGet(
                applicationBuilder,
                new HttpGetMiddlewareOptions());

        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder,
            IHttpGetMiddlewareOptions options)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UseMiddleware<HttpGetMiddleware>(options);
        }
    }
}
