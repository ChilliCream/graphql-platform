using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Adapters.OpenApi;

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    string HttpMethod,
    RoutePattern Route,
    ImmutableArray<OpenApiEndpointParameterDescriptor> Parameters,
    bool HasRouteParameters,
    string? VariableFilledThroughBody,
    string ResponseNameToExtract);

internal sealed record OpenApiEndpointParameterDescriptor(
    string ParameterKey,
    string VariableName,
    ImmutableArray<string>? InputObjectPath,
    ITypeDefinition Type,
    OpenApiEndpointParameterType ParameterType);

internal enum OpenApiEndpointParameterType
{
    Route,
    Query
}
