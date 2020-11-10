#if NETCOREAPP3_1 || NET5_0
using HotChocolate.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class RoutingEndpointConventionBuilderExtensions
    {
        public static TBuilder WithToolOptions<TBuilder>(this TBuilder builder, ToolOptions options)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.WithMetadata(options);
        }
    }
}
#endif
