using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Exporters.OpenApi;

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    string HttpMethod,
    RoutePattern Route,
    IReadOnlyList<OpenApiRouteSegmentParameter> RouteParameters,
    IReadOnlyList<OpenApiRouteSegmentParameter> QueryParameters,
    string? VariableFilledThroughBody,
    string ResponseNameToExtract);

// internal sealed class SomeTrie : Dictionary<string, SomeTrie>
// {
//     public SomeTrieLeaf? Leaf { get; init; }
// }
//
// internal sealed class SomeTrieLeaf(ParameterType parameterType, IInputType inputType)
// {
//     public ParameterType Type { get; } = parameterType;
//
//     public IInputType InputType { get; } = inputType;
// }
//
// internal enum ParameterType
// {
//     Route,
//     Query
// }
