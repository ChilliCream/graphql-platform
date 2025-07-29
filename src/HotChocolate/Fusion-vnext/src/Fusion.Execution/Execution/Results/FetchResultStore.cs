using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
internal sealed class FetchResultStore : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ValueCompletion _valueCompletion;
    private readonly ISchemaDefinition _schema;
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private readonly ObjectResult _root;

    private readonly ImmutableArray<IError> _errors = [];

    // TODO : attach resources to result object.
    private readonly ConcurrentStack<IDisposable> _memory = [];

    public FetchResultStore(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        Operation operation,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(operation);

        _schema = schema;
        _operation = operation;
        _includeFlags = includeFlags;
        _valueCompletion = new ValueCompletion(schema, resultPoolSession, ErrorHandling.Propagate, 32, includeFlags);
        _root = resultPoolSession.RentObjectResult();
        _root.Initialize(resultPoolSession, operation.RootSelectionSet, includeFlags);
    }

    public ObjectResult Data => _root;

    public ImmutableArray<IError> Errors => _errors;

    public ConcurrentStack<IDisposable> MemoryOwners => _memory;

    public bool AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);

        if (results.Length == 0)
        {
            throw new ArgumentException(
                "The results span must contain at least one result.",
                nameof(results));
        }

        var startElements = ArrayPool<JsonElement>.Shared.Rent(results.Length);
        var startElementsSpan = startElements.AsSpan()[..results.Length];

        try
        {
            ref var result = ref MemoryMarshal.GetReference(results);
            ref var startElement = ref MemoryMarshal.GetReference(startElements);
            ref var end = ref Unsafe.Add(ref result, results.Length);

            while (Unsafe.IsAddressLessThan(ref result, ref end))
            {
                // we need to track the result objects as they used rented memory.
                _memory.Push(result);

                startElement = GetStartElement(sourcePath, result.Data);
                result = ref Unsafe.Add(ref result, 1)!;
                startElement = ref Unsafe.Add(ref startElement, 1);
            }

            return SaveSafe(results, startElementsSpan);
        }
        finally
        {
            ArrayPool<JsonElement>.Shared.Return(startElements);
        }
    }

    public void AddPartialResults(ObjectResult result, ReadOnlySpan<Selection> selections)
    {
        _lock.EnterWriteLock();

        try
        {
            foreach (var selection in selections)
            {
                if (!selection.IsIncluded(_includeFlags))
                {
                    continue;
                }

                result.MoveFieldTo(selection.ResponseName, _root);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private bool SaveSafe(
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<JsonElement> startElements)
    {
        _lock.EnterWriteLock();

        try
        {
            ref var result = ref MemoryMarshal.GetReference(results);
            ref var startElement = ref MemoryMarshal.GetReference(startElements);
            ref var end = ref Unsafe.Add(ref result, results.Length);

            while (Unsafe.IsAddressLessThan(ref result, ref end))
            {
                if (result.Path.IsRoot)
                {
                    var selectionSet = _operation.RootSelectionSet;
                    if (!_valueCompletion.BuildResult(selectionSet, result, startElement, _root))
                    {
                        return false;
                    }
                }
                else
                {
                    var startResult = GetStartObjectResult(result.Path);
                    if (!_valueCompletion.BuildResult(startResult.SelectionSet, result, startElement, startResult))
                    {
                        return false;
                    }
                }

                result = ref Unsafe.Add(ref result, 1)!;
                startElement = ref Unsafe.Add(ref startElement, 1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return true;
    }

    public ImmutableArray<VariableValues> CreateVariableValueSets(
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        _lock.EnterReadLock();

        try
        {
            var current = new List<ObjectResult> { _root };
            var next = new List<ObjectResult>();

            for (var i = selectionSet.Segments.Length - 1; i >= 0; i--)
            {
                var segment = selectionSet.Segments[i];
                foreach (var result in current)
                {
                    if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
                    {
                        if (result.TryGetValue(IntrospectionFieldNames.TypeName, out var value)
                            && value is LeafFieldResult leaf
                            && (leaf.Value.GetString()?.Equals(segment.Name) ?? false))
                        {
                            next.Add(result);
                        }
                    }
                    else if (segment.Kind is SelectionPathSegmentKind.Field)
                    {
                        if (!result.TryGetValue(segment.Name, out var value) || value.HasNullValue)
                        {
                            continue;
                        }

                        if (value is ListFieldResult { Value: { } list })
                        {
                            next.AddRange(UnrollLists(list));
                            continue;
                        }

                        if (value is ObjectFieldResult objectField)
                        {
                            next.Add(objectField.Value!);
                            continue;
                        }

                        // TODO : Better error
                        throw new NotSupportedException("Must be list or object.");
                    }
                }

                (next, current) = (current, next);
                next.Clear();
            }

            PooledArrayWriter? buffer = null;
            VariableValues[]? variableValueSets = null;
            var nextIndex = 0;

            foreach (var result in current)
            {
                var variables = MapRequirements(
                    result,
                    requestVariables,
                    requiredData,
                    ref buffer);

                if (variables is not null)
                {
                    variableValueSets ??= new VariableValues[current.Count];
                    variableValueSets[nextIndex++] = new VariableValues(result.Path, variables);
                }
            }

            if (variableValueSets?.Length > 0)
            {
                Array.Resize(ref variableValueSets, nextIndex);
            }

            if (buffer is not null)
            {
                _memory.Push(buffer);
            }

            return variableValueSets is not null
                ? ImmutableCollectionsMarshal.AsImmutableArray(variableValueSets)
                : [];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private ObjectValueNode? MapRequirements(
        ObjectResult result,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        ref PooledArrayWriter? buffer)
    {
        var fields = new List<ObjectFieldNode>(requestVariables.Count + requiredData.Length);
        fields.AddRange(requestVariables);

        foreach (var requirement in requiredData)
        {
            var field = MapRequirement(result, requirement.Key, requirement.Map, ref buffer);

            if (field is null)
            {
                return null;
            }

            if (field.Value.Kind == SyntaxKind.NullValue && requirement.Type.Kind == SyntaxKind.NonNullType)
            {
                return null;
            }

            fields.Add(field);
        }

        return new ObjectValueNode(fields);
    }

    private ObjectFieldNode? MapRequirement(
        ObjectResult result,
        string key,
        IValueSelectionNode path,
        ref PooledArrayWriter? buffer)
    {
        var value = ResultDataMapper.Map(result, path, _schema, ref buffer);
        return value is null ? null : new ObjectFieldNode(key, value);
    }

    private static IEnumerable<ObjectResult> UnrollLists(ListResult list)
    {
        if (list is NestedListResult nestedList)
        {
            foreach (var item in nestedList.Items)
            {
                if (item is null)
                {
                    continue;
                }

                foreach (var result in UnrollLists(item))
                {
                    yield return result;
                }
            }
        }
        else if (list is ObjectListResult objectList)
        {
            foreach (var result in objectList.Items)
            {
                if (result is null)
                {
                    continue;
                }

                yield return result;
            }
        }
        else
        {
            throw new NotSupportedException(
                "Only nested lists and object lists are supported.");
        }
    }

    public PooledArrayWriter CreateRentedBuffer()
    {
        var buffer = new PooledArrayWriter();
        _memory.Push(buffer);
        return buffer;
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
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment.Name, out current))
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

        if (result is ObjectResult objectResult && path is NamePathSegment nameSegment)
        {
            if (!objectResult.TryGetValue(nameSegment.Name, out var field))
            {
                return null;
            }

            return field switch
            {
                ObjectFieldResult objectFieldResult => objectFieldResult.Value,
                ListFieldResult listFieldResult => listFieldResult.Value,
                null => null,
                _ => throw new InvalidOperationException($"The path segment '{parent}' does not exist in the data.")
            };
        }

        if (path is IndexerPathSegment indexSegment)
        {
            switch (result)
            {
                case NestedListResult listResult:
                    if (listResult.Items.Count <= indexSegment.Index)
                    {
                        throw new InvalidOperationException(
                            $"The path segment '{indexSegment}' does not exist in the data.");
                    }
                    return listResult.Items[indexSegment.Index];

                case ObjectListResult listResult:
                    if (listResult.Items.Count <= indexSegment.Index)
                    {
                        throw new InvalidOperationException(
                            $"The path segment '{indexSegment}' does not exist in the data.");
                    }
                    return listResult.Items[indexSegment.Index];
            }
        }

        throw new InvalidOperationException(
            $"The path segment '{parent}' does not exist in the data.");
    }

    public void Dispose()
    {
        _lock.Dispose();
        _memory.Clear();
    }
}
