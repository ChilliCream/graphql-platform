using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public static class HttpGetSchemaApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder) =>
            UseGraphQLHttpGetSchema(applicationBuilder, new PathString());

        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder,
            PathString path)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            return applicationBuilder.Map(
                path,
                b => b.UseMiddleware<HttpGetSchemaMiddleware>());
        }
    }
}
