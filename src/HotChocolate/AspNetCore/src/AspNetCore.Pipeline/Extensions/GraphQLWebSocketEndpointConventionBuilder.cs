using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Extensions;

/// <summary>
/// Represents the endpoint convention builder for GraphQL over WebSockets.
/// </summary>
public sealed class GraphQLWebSocketEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _builder;

    internal GraphQLWebSocketEndpointConventionBuilder(IEndpointConventionBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
        => _builder.Add(convention);
}
