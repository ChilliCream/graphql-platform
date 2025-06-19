using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
internal sealed class FetchResultStore
{
    private readonly ISchemaDefinition _schema;
    private readonly ResultPoolSession _resultPoolSession;
    private readonly ValueCompletion _valueCompletion;
    private readonly Operation _operation = default!;
    private readonly ObjectResult _root;
    private readonly uint _includeFlags;

    public FetchResultStore(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        Operation operation,
        uint includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(operation);

        _schema = schema;
        _resultPoolSession = resultPoolSession;
        _valueCompletion = new ValueCompletion(schema, resultPoolSession, ErrorHandling.Propagate, 32, includeFlags);
        _operation = operation;
        _root = resultPoolSession.RentObjectResult();
        _includeFlags = includeFlags;
    }

    public bool Save(
        SelectionPath sourcePath,
        SourceSchemaResult result)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(result);

        if (result.Path.IsRoot)
        {
            var selectionSet = _operation.RootSelectionSet;

            if (!_root.IsInitialized)
            {
                _root.Initialize(_resultPoolSession, selectionSet, _includeFlags);
            }

            var start = GetStartElement(sourcePath, result.Data);
            return _valueCompletion.BuildResult(selectionSet, result, start, _root);
        }
        else
        {
            var start = GetStartElement(sourcePath, result.Data);
            var startResult = GetStartObjectResult(result.Path);
            return _valueCompletion.BuildResult(startResult.SelectionSet, result, start, startResult);
        }
    }

    public ImmutableArray<VariableValues> CreateVariableValueSets(
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ImmutableArray<OperationRequirement> requiredData)
    {
        return [];
    }

    private static JsonElement GetStartElement(SelectionPath sourcePath, JsonElement data)
    {
        if (sourcePath.IsRoot)
        {
            return data;
        }

        var current = data;

        for (var i = sourcePath.Segments.Length - 1; i >= 0; i--)
        {
            var segment = sourcePath.Segments[i];
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment.Name, out current))
            {
                throw new InvalidOperationException(
                    $"The path segment '{segment.Name}' does not exist in the data.");
            }
        }

        return current;
    }

    private ObjectResult GetStartObjectResult(Path path)
    {
        var result = GetStartResult(path);

        if (result is ObjectResult objectResult)
        {
            return objectResult;
        }

        throw new InvalidOperationException(
            $"The path segment '{path}' does not exist in the data.");
    }

    private ResultData? GetStartResult(Path path)
    {
        if (path.IsRoot)
        {
            return _root;
        }

        var parent = path.Parent;
        var result = GetStartResult(parent);

        if (result is ObjectResult objectResult
            && parent is NamePathSegment nameSegment)
        {
            return objectResult[nameSegment.Name];
        }

        if (parent is IndexerPathSegment indexSegment)
        {
            switch (result)
            {
                case NestedListResult listResult:
                    return listResult.Items[indexSegment.Index];

                case ObjectListResult listResult:
                    return listResult.Items[indexSegment.Index];
            }
        }

        throw new InvalidOperationException(
            $"The path segment '{parent}' does not exist in the data.");
    }
}
