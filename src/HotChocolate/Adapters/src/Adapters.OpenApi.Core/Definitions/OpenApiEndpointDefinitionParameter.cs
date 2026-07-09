using System.Collections.Immutable;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiEndpointDefinitionParameter(
    string Key,
    string VariableName,
    ImmutableArray<string>? InputObjectPath);
