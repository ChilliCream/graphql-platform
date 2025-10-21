using HotChocolate.Language;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class ExecutableOpenApiDocument(
    DocumentNode document,
    string httpMethod,
    RoutePattern route)
{
    public DocumentNode Document { get; } = document;

    public string HttpMethod { get; } = httpMethod;

    public RoutePattern Route { get; } = route;
}
