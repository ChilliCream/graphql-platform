using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiEndpointDefinition : IOpenApiDefinition
{
    internal OpenApiEndpointDefinition(
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

    public static OpenApiEndpointDefinition From(
        OpenApiEndpointSettingsDto settings,
        string httpMethod,
        string route,
        DocumentNode document)
    {
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault() ??
            throw new ArgumentException("The document must contain exactly one operation definition.",
                nameof(document));

        var fragmentReferences = FragmentReferenceFinder.Find(document);

        return new OpenApiEndpointDefinition(
            httpMethod,
            route,
            settings.Description,
            settings.RouteParameters,
            settings.QueryParameters,
            settings.BodyVariableName,
            document,
            operationDefinition,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    public OpenApiEndpointSettingsDto ToDto()
        => new OpenApiEndpointSettingsDto(Description, RouteParameters, QueryParameters, BodyVariableName);
}
