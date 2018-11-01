using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
            this IApplicationBuilder applicationBuilder,
            PathString route)
        {
            if (route == null)
            {
                return UseGraphQL(applicationBuilder);
            }

            return applicationBuilder.Map(route,
                app => app.UseMiddleware<PostQueryMiddleware>()
                    .UseMiddleware<GetQueryMiddleware>()
                    .UseMiddleware<SubscriptionMiddleware>());
        }
    }
}
