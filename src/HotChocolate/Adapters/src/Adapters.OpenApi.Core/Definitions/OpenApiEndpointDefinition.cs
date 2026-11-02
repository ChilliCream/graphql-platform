using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiEndpointDefinition(
    string HttpMethod,
    string Route,
    string? Description,
    ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters,
    ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters,
    string? BodyVariableName,
    DocumentNode Document,
    OperationDefinitionNode OperationDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDefinition;
