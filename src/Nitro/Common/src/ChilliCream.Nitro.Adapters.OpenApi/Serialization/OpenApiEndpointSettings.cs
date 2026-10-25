using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi;

namespace ChilliCream.Nitro.Adapters.OpenApi.Serialization;

public sealed record OpenApiEndpointSettings(
    string? Description,
    ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters = default,
    ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters = default,
    string? BodyVariableName = null)
{
    public ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters { get; init; } =
        RouteParameters.IsDefault ? [] : RouteParameters;

    public ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters { get; init; } =
        QueryParameters.IsDefault ? [] : QueryParameters;
}
