using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public static class SubscriptionApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLSubscriptions(
            this IApplicationBuilder applicationBuilder)
        {
            return UseGraphQLSubscriptions(
                applicationBuilder,
                new SubscriptionMiddlewareOptions());
        }

        public static IApplicationBuilder UseGraphQLSubscriptions(
            this IApplicationBuilder applicationBuilder,
            SubscriptionMiddlewareOptions options)
        {
            applicationBuilder.UseMiddleware<SubscriptionMiddleware>(options);
            return applicationBuilder;
        }
    }
}
