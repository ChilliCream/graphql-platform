using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Exporters.OpenApi;

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    string HttpMethod,
    RoutePattern Route,
    ImmutableArray<OpenApiEndpointParameterDescriptor> Parameters,
    bool HasRouteParameters,
    string? VariableFilledThroughBody,
    string ResponseNameToExtract);

// TODO: Handle values nested in input objects
internal sealed record OpenApiEndpointParameterDescriptor(
    string ParameterKey,
    string VariableName,
    ITypeDefinition Type,
    OpenApiEndpointParameterType ParameterType);

internal enum OpenApiEndpointParameterType
{
    Route,
    Query
}
