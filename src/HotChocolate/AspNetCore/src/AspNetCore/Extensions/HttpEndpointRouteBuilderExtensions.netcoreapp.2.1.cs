#if NETCOREAPP2_1
using System;
using HotChocolate;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpEndpointRouteBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            PathString pathMatch = default,
            NameString schemaName = default)
        {
            if (applicationBuilder is null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            NameString schemaNameOrDefault = schemaName.HasValue ? schemaName : Schema.DefaultName;

            return applicationBuilder.Map(
                pathMatch,
                app =>
                {
                    app.UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpGetSchemaMiddleware>(schemaNameOrDefault);
                    app.UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault);
                });
        }

        private static IFileProvider CreateFileProvider()
        {
            Type type = typeof(HttpEndpointRouteBuilderExtensions);
            string resourceNamespace = typeof(MiddlewareBase).Namespace + ".Resources";

            return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
        }
    }
}
#endif
