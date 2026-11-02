namespace HotChocolate.AspNetCore;

internal sealed class GraphQLServerOptionsOverride(Action<GraphQLServerOptions> configure)
{
    public void Apply(GraphQLServerOptions options) => configure(options);
}
