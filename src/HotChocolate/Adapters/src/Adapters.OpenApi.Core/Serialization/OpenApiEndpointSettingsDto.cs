using System.Collections.Immutable;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiEndpointSettingsDto(
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
