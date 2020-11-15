using HotChocolate.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class RoutingEndpointConventionBuilderExtensions
    {
        public static TBuilder WithOptions<TBuilder>(
            this TBuilder builder,
            GraphQLServerOptions serverOptions)
            where TBuilder : IEndpointConventionBuilder =>
            builder.WithMetadata(serverOptions);
    }
}
