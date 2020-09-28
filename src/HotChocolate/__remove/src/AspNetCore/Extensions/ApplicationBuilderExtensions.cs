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

            applicationBuilder
                .UseGraphQLHttpPost(new HttpPostMiddlewareOptions
                {
                    Path = options.Path,
                    ParserOptions = options.ParserOptions,
                    MaxRequestSize = options.MaxRequestSize
                })
                .UseGraphQLHttpGet(new HttpGetMiddlewareOptions
                {
                    Path = options.Path
                })
                .UseGraphQLHttpGetSchema(new HttpGetSchemaMiddlewareOptions
                {
                    Path = options.Path.Add(new PathString("/schema"))
                });

            if (options.EnableSubscriptions)
            {
                applicationBuilder.UseGraphQLSubscriptions(
                    new SubscriptionMiddlewareOptions
                    {
                        ParserOptions = options.ParserOptions,
                        Path = options.SubscriptionPath
                    });
            }

            return applicationBuilder;
        }
    }
}
