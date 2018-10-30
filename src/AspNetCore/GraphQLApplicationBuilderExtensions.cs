using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate
{
    public static class GraphQLApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<PostQueryMiddleware>()
                .UseMiddleware<GetQueryMiddleware>()
                .UseMiddleware<SubscriptionMiddleware>();
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder, string route)
        {
            if (route == null)
            {
                return UseGraphQL(applicationBuilder);
            }

            return applicationBuilder.Map("/" + route.Trim('/'),
                app => app.UseMiddleware<PostQueryMiddleware>()
                    .UseMiddleware<GetQueryMiddleware>()
                    .UseMiddleware<SubscriptionMiddleware>());
        }
    }
}
