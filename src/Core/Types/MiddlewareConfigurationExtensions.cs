using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static class MiddlewareConfigurationExtensions
    {
        public static IMiddlewareConfiguration Use<TMiddleware>(
            this IMiddlewareConfiguration configuration)
            where TMiddleware : class
        {
            return configuration.Use(
                ClassMiddlewareFactory.Create<TMiddleware>());
        }
    }
}
