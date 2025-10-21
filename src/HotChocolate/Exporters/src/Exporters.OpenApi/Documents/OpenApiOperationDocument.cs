using HotChocolate.Language;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiOperationDocument(
    string id,
    OperationDefinitionNode operationDefinition,
    string httpMethod,
    RoutePattern route) : IOpenApiDocument
{
    public string Id { get; } = id;

    public OperationDefinitionNode OperationDefinition { get; } = operationDefinition;

    public string HttpMethod { get; } = httpMethod;

    public RoutePattern Route { get; } = route;
}
