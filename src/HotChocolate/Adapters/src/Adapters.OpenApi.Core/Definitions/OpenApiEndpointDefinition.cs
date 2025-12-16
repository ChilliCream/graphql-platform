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
    Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDefinition
{
    public OperationDefinitionNode OperationDefinition => Document.Definitions.OfType<OperationDefinitionNode>().First();

    public static OpenApiEndpointDefinition From(
        OpenApiEndpointSettingsDto settings,
        string httpMethod,
        string route,
        DocumentNode document)
    {
        var fragmentReferences = FragmentReferenceFinder.Find(document);

        return new OpenApiEndpointDefinition(
            httpMethod,
            route,
            settings.Description,
            settings.RouteParameters,
            settings.QueryParameters,
            settings.BodyVariableName,
            document,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    public OpenApiEndpointSettingsDto ToDto()
        => new OpenApiEndpointSettingsDto(Description, RouteParameters, QueryParameters, BodyVariableName);
}

public sealed record OpenApiEndpointDefinitionParameter(
    string Key,
    string VariableName,
    ImmutableArray<string>? InputObjectPath);
