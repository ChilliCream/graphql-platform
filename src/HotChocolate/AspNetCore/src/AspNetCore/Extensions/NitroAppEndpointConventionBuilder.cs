using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Extensions;

/// <summary>
/// Represents the endpoint convention builder for Nitro.
/// </summary>
public sealed class NitroAppEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _builder;

    internal NitroAppEndpointConventionBuilder(IEndpointConventionBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention) =>
        _builder.Add(convention);
}
