using HotChocolate.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class GraphQLEndpointConventionBuilderExtensions
    {
        public static TBuilder WithToolOptions<TBuilder>(this TBuilder builder, ToolOptions options)
            where TBuilder : IGraphQLEndpointConventionBuilder
        {
            return builder.WithMetadata(options);
        }
    }
}
