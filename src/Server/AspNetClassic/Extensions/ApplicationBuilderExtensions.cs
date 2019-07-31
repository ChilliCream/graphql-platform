using System;
using Microsoft.Owin;
using IApplicationBuilder = Owin.IAppBuilder;

namespace HotChocolate.AspNetClassic
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider)
        {
            return applicationBuilder
                .UseGraphQL(serviceProvider, new QueryMiddlewareOptions());
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            PathString path)
        {
            var options = new QueryMiddlewareOptions
            {
                Path = path.HasValue ? path : new PathString("/")
            };

            return applicationBuilder
                .UseGraphQL(serviceProvider, options);
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            QueryMiddlewareOptions options)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UseGraphQLHttpPost(serviceProvider,
                    new HttpPostMiddlewareOptions
                    {
                        Path = options.Path,
                        ParserOptions = options.ParserOptions,
                        MaxRequestSize = options.MaxRequestSize
                    })
                .UseGraphQLHttpGet(serviceProvider,
                    new HttpGetMiddlewareOptions
                    {
                        Path = options.Path
                    })
                .UseGraphQLHttpGetSchema(serviceProvider,
                    new HttpGetSchemaMiddlewareOptions
                    {
                        Path = options.Path.Add(new PathString("/schema"))
                    });
        }
    }
}
