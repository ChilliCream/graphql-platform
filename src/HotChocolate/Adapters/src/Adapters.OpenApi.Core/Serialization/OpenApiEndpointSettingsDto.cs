using System.Collections.Immutable;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiEndpointSettingsDto(
    string? Description,
    ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters,
    ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters,
    string? BodyVariableName);
