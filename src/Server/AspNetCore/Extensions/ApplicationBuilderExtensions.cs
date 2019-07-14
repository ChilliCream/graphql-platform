using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Subscriptions;

namespace HotChocolate.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder
                .UseGraphQL(new QueryMiddlewareOptions());
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            PathString path)
        {
            var options = new QueryMiddlewareOptions
            {
                Path = path.HasValue ? path : new PathString("/")
            };

            return applicationBuilder
                .UseGraphQL(options);
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            QueryMiddlewareOptions options)
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
                .UseMiddleware<PostQueryMiddleware>(options)
                .UseMiddleware<GetQueryMiddleware>(options)
                .UseGraphQLSubscriptions(new SubscriptionMiddlewareOptions
                {
                    ParserOptions = options.ParserOptions,
                    SubscriptionPath = options.SubscriptionPath
                })
                .UseMiddleware<SchemaMiddleware>(options);
        }
    }
}
