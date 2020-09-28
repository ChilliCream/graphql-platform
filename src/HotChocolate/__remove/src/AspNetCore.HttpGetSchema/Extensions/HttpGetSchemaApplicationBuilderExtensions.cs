using System;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore
{
    public static class HttpGetSchemaApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            return UseGraphQLHttpGetSchema(
                applicationBuilder,
                new HttpGetSchemaMiddlewareOptions());
        }

        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder,
            IHttpGetSchemaMiddlewareOptions options)
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
                .UseMiddleware<HttpGetSchemaMiddleware>(options);
        }
    }
}
