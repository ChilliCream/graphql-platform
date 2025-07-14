using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Extensions;

/// <summary>
/// Represents the endpoint convention builder for GraphQL HTTP requests.
/// </summary>
public sealed class GraphQLHttpEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _builder;

    internal GraphQLHttpEndpointConventionBuilder(IEndpointConventionBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention) =>
        _builder.Add(convention);
}
