using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiEndpointDefinition : IOpenApiDefinition
{
    public OpenApiEndpointDefinition(
        string httpMethod,
        string route,
        string? description,
        ImmutableArray<OpenApiEndpointDefinitionParameter> routeParameters,
        ImmutableArray<OpenApiEndpointDefinitionParameter> queryParameters,
        string? bodyVariableName,
        DocumentNode document,
        OperationDefinitionNode operationDefinition,
        Dictionary<string, FragmentDefinitionNode> localFragmentsByName,
        HashSet<string> externalFragmentReferences)
    {
        HttpMethod = httpMethod;
        Route = route;
        Description = description;
        RouteParameters = routeParameters;
        QueryParameters = queryParameters;
        BodyVariableName = bodyVariableName;
        Document = document;
        OperationDefinition = operationDefinition;
        LocalFragmentsByName = localFragmentsByName;
        ExternalFragmentReferences = externalFragmentReferences;
    }

    public string HttpMethod { get; }

    public string Route { get; }

    public string? Description { get; }

    public ImmutableArray<OpenApiEndpointDefinitionParameter> RouteParameters { get; }

    public ImmutableArray<OpenApiEndpointDefinitionParameter> QueryParameters { get; }

    public string? BodyVariableName { get; }

    public DocumentNode Document { get; }

    public OperationDefinitionNode OperationDefinition { get; }

    public Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName { get; }

    public HashSet<string> ExternalFragmentReferences { get; }
}
