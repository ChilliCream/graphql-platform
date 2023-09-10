using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Extensions;

/// <summary>
/// Represents the endpoint convention builder for Banana Cake Pop.
/// </summary>
public sealed class BananaCakePopEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _builder;

    internal BananaCakePopEndpointConventionBuilder(IEndpointConventionBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention) =>
        _builder.Add(convention);
}
