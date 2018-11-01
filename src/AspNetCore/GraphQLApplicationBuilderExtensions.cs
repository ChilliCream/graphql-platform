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
                .UseGraphQLSubscriptions("/ws");
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
                    .UseMiddleware<GetQueryMiddleware>())
                .UseGraphQLSubscriptions(route + "/ws");
        }

        private static IApplicationBuilder UseGraphQLSubscriptions(
            this IApplicationBuilder applicationBuilder,
            PathString route)
        {
            return applicationBuilder.Map(route,
                app => app.UseMiddleware<SubscriptionMiddleware>());
        }
    }
}
