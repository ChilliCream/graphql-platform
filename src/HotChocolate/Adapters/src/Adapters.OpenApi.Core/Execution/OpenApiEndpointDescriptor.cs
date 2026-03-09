using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Adapters.OpenApi;

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    bool HasValidDocument,
    string HttpMethod,
    RoutePattern Route,
    VariableValueInsertionTrie ParameterTrie,
    string? VariableFilledThroughBody,
    string ResponseNameToExtract);

internal interface IVariableValueInsertionTrieSegment;

internal sealed class VariableValueInsertionTrie
    : Dictionary<string, IVariableValueInsertionTrieSegment>,
        IVariableValueInsertionTrieSegment;

internal sealed record VariableValueInsertionTrieLeaf(
    string ParameterKey,
    ITypeDefinition Type,
    OpenApiEndpointParameterType ParameterType,
    bool HasDefaultValue) : IVariableValueInsertionTrieSegment;

internal enum OpenApiEndpointParameterType
{
    Route,
    Query
}
