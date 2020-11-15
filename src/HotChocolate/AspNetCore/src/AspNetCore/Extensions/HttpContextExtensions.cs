using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class HttpContextExtensions
    {
        public static GraphQLServerOptions? GetGraphQLServerOptions(this HttpContext context) =>
            context.GetEndpoint()?.Metadata.GetMetadata<GraphQLServerOptions>();

        public static GraphQLToolOptions? GetGraphQLToolOptions(this HttpContext context) =>
            GetGraphQLServerOptions(context)?.Tool;
    }
}
