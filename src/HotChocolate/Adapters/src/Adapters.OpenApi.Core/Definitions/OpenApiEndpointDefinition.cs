using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiEndpointDefinition(
    string HttpMethod,
    string Route,
    string? Description,
    ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters,
    ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters,
    OpenApiEndpointDefinitionParameter? BodyParameter,
    DocumentNode Document,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDefinition
{
    public OperationDefinitionNode OperationDefinition => Document.Definitions.OfType<OperationDefinitionNode>().First();
}

public sealed record OpenApiEndpointDefinitionParameter(
    string Key,
    string VariableName,
    ImmutableArray<string>? InputObjectPath);
