using System;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore
{
    public static class HttpPostApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpPost(
            this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            return UseGraphQLHttpPost(
                applicationBuilder,
                new HttpPostMiddlewareOptions());
        }

        public static IApplicationBuilder UseGraphQLHttpPost(
            this IApplicationBuilder applicationBuilder,
            IHttpPostMiddlewareOptions options)
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
                .UseMiddleware<HttpPostMiddleware>(options);
        }
    }
}
