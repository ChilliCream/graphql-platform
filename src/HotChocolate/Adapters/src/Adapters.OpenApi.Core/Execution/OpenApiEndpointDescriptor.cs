using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Adapters.OpenApi;

internal sealed record OpenApiEndpointDescriptor(
    DocumentNode Document,
    IReadOnlyList<IError> DocumentErrors,
    string HttpMethod,
    RoutePattern Route,
    VariableValueInsertionTrie ParameterTrie,
    string? VariableFilledThroughBody,
    OpenApiResponseBodySelection ResponseBodySelection)
{
    public bool HasValidDocument => DocumentErrors.Count == 0;
}

internal interface IVariableValueInsertionTrieSegment;

internal sealed class VariableValueInsertionTrie
    : Dictionary<string, IVariableValueInsertionTrieSegment>,
        IVariableValueInsertionTrieSegment;

internal sealed record VariableValueInsertionTrieLeaf(
    string ParameterKey,
    ITypeDefinition NamedType,
    OpenApiEndpointParameterType ParameterType,
    bool HasDefaultValue,
    bool IsNonNullType) : IVariableValueInsertionTrieSegment;

internal enum OpenApiEndpointParameterType
{
    Route,
    Query
}
