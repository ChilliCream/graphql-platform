using Microsoft.AspNetCore.Builder;

namespace HotChocolate
{
    public static class GraphQLApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<QueryMiddleware>();
        }

        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder, string route)
        {
            if (route == null)
            {
                return UseGraphQL(applicationBuilder);
            }
            return applicationBuilder.UseMiddleware<QueryMiddleware>(route);
        }
    }
}
