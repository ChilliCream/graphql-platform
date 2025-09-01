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
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// TODO: we must make this thread-safe
internal sealed class FetchResultStore : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ISchemaDefinition _schema;
    private readonly Operation _operation;
    private readonly ErrorHandlingMode _errorHandling;
    private readonly ulong _includeFlags;
    private readonly ConcurrentStack<IDisposable> _memory = [];
    private ObjectResult _root = null!;
    private ValueCompletion _valueCompletion = null!;
    private List<IError> _errors = null!;
    private bool _disposed;

    public FetchResultStore(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        Operation operation,
        ErrorHandlingMode errorHandling,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(operation);

        _schema = schema;
        _operation = operation;
        _errorHandling = errorHandling;
        _includeFlags = includeFlags;

        Reset(resultPoolSession);
    }

    public void Reset(ResultPoolSession resultPoolSession)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _root = resultPoolSession.RentObjectResult();
        _root.Initialize(resultPoolSession, _operation.RootSelectionSet, _includeFlags);
        _errors = [];

        _valueCompletion = new ValueCompletion(
            _schema,
            resultPoolSession,
            _errorHandling,
            32,
            _includeFlags,
            _errors);
    }

    public ObjectResult Data => _root;

    public List<IError> Errors => _errors;

    public ConcurrentStack<IDisposable> MemoryOwners => _memory;

    public bool AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<string> responseNames)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sourcePath);

        if (results.Length == 0)
        {
            throw new ArgumentException(
                "The results span must contain at least one result.",
                nameof(results));
        }

        var dataElements = ArrayPool<JsonElement>.Shared.Rent(results.Length);
        var errorTries = ArrayPool<ErrorTrie?>.Shared.Rent(results.Length);
        var dataElementsSpan = dataElements.AsSpan()[..results.Length];
        var errorTriesSpan = errorTries.AsSpan()[..results.Length];

        try
        {
            ref var result = ref MemoryMarshal.GetReference(results);
            ref var dataElement = ref MemoryMarshal.GetReference(dataElementsSpan);
            ref var errorTrie = ref MemoryMarshal.GetReference(errorTriesSpan);
            ref var end = ref Unsafe.Add(ref result, results.Length);

            while (Unsafe.IsAddressLessThan(ref result, ref end))
            {
                // we need to track the result objects as they used rented memory.
                _memory.Push(result);

                if (result.Errors?.RootErrors is { Length: > 0 } rootErrors)
                {
                    _errors.AddRange(rootErrors);
                }

                dataElement = GetDataElement(sourcePath, result.Data);
                errorTrie = GetErrorTrie(sourcePath, result.Errors?.Trie);

                result = ref Unsafe.Add(ref result, 1)!;
                dataElement = ref Unsafe.Add(ref dataElement, 1);
                errorTrie = ref Unsafe.Add(ref errorTrie, 1)!;
            }

            return SaveSafe(results, dataElementsSpan, errorTriesSpan, responseNames);
        }
        finally
        {
            ArrayPool<JsonElement>.Shared.Return(dataElements);
            ArrayPool<ErrorTrie?>.Shared.Return(errorTries);
        }
    }

    public void AddPartialResults(ObjectResult result, ReadOnlySpan<Selection> selections)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(result);

        if (selections.Length == 0)
        {
            throw new ArgumentException(
                "The selections span must contain at least one selection.",
                nameof(selections));
        }

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

    public bool AddErrors(IError error, ReadOnlySpan<string> responseNames, params ReadOnlySpan<Path> paths)
    {
        _lock.EnterWriteLock();

        try
        {
            ref var path = ref MemoryMarshal.GetReference(paths);
            ref var end = ref Unsafe.Add(ref path, paths.Length);

            while (Unsafe.IsAddressLessThan(ref path, ref end))
            {
                if (_root.IsInvalidated)
                {
                    return false;
                }

                var objectResult = path.IsRoot ? _root : GetStartObjectResult(path);

#pragma warning disable RCS1146
                // disabled warning for readability of the condition.
                if (objectResult is null || objectResult.IsInvalidated)
                {
                    goto AddErrors_Next;
                }
#pragma warning restore RCS1146

                var canExecutionContinue = _valueCompletion.BuildErrorResult(objectResult, responseNames, error, path);

                if (!canExecutionContinue)
                {
                    _root.IsInvalidated = true;

                    return false;
                }

                AddErrors_Next:
                path = ref Unsafe.Add(ref path, 1)!;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return true;
    }

    private bool SaveSafe(
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<JsonElement> dataElements,
        ReadOnlySpan<ErrorTrie?> errorTries,
        ReadOnlySpan<string> responseNames)
    {
        _lock.EnterWriteLock();

        try
        {
            ref var result = ref MemoryMarshal.GetReference(results);
            ref var data = ref MemoryMarshal.GetReference(dataElements);
            ref var errorTrie = ref MemoryMarshal.GetReference(errorTries);
            ref var end = ref Unsafe.Add(ref result, results.Length);

            while (Unsafe.IsAddressLessThan(ref result, ref end))
            {
                if (_root.IsInvalidated)
                {
                    return false;
                }

#pragma warning disable RCS1146
                // disabled warning for readability of the condition
                var objectResult = result.Path.IsRoot ? _root : GetStartObjectResult(result.Path);
                if (objectResult is null || objectResult.IsInvalidated)
                {
                    goto SaveSafe_Next;
                }
#pragma warning restore RCS1146

                var selectionSet = result.Path.IsRoot ? _operation.RootSelectionSet : objectResult.SelectionSet;
                var canExecutionContinue = _valueCompletion.BuildResult(
                    selectionSet,
                    data,
                    errorTrie,
                    responseNames,
                    objectResult);

                if (!canExecutionContinue)
                {
                    _root.IsInvalidated = true;

                    return false;
                }

                SaveSafe_Next:
                result = ref Unsafe.Add(ref result, 1)!;
                data = ref Unsafe.Add(ref data, 1);
                errorTrie = ref Unsafe.Add(ref errorTrie, 1)!;
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(requestVariables);

        if (requiredData.Length == 0)
        {
            throw new ArgumentException(
                "The required data span must contain at least one requirement.",
                nameof(requiredData));
        }

        _lock.EnterReadLock();

        try
        {
            var current = new List<ObjectResult> { _root };
            var next = new List<ObjectResult>();

            for (var i = 0; i < selectionSet.Segments.Length; i++)
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

                if (current.Count == 0)
                {
                    return [];
                }
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
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new PooledArrayWriter();
        _memory.Push(buffer);
        return buffer;
    }

    private static JsonElement GetDataElement(SelectionPath sourcePath, JsonElement data)
    {
        if (sourcePath.IsRoot)
        {
            return data;
        }

        var current = data;

        for (var i = 0; i < sourcePath.Segments.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            var segment = sourcePath.Segments[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field:
                    if (!current.TryGetProperty(segment.Name, out current))
                    {
                        return default;
                    }

                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    if (!current.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var typeNameProperty)
                            || typeNameProperty.ValueKind != JsonValueKind.String)
                    {
                        return default;
                    }

                    var typeName = typeNameProperty.GetString()!;

                    if (typeName != segment.Name)
                    {
                        return default;
                    }

                    break;

                default:
                    throw new NotImplementedException($"Segment kind {segment.Kind} is not supported.");
            }
        }

        return current;
    }

    private static ErrorTrie? GetErrorTrie(SelectionPath sourcePath, ErrorTrie? errors)
    {
        if (errors is null || sourcePath.IsRoot)
        {
            return errors;
        }

        var current = errors;

        for (var i = 0; i < sourcePath.Segments.Length; i++)
        {
            var segment = sourcePath.Segments[i];

            if (!current.TryGetValue(segment.Name, out current))
            {
                return null;
            }
        }

        return current;
    }

    private ObjectResult? GetStartObjectResult(Path path)
    {
        var result = GetStartResult(path);

        if (result is ObjectResult objectResult)
        {
            return objectResult;
        }

        return null;
    }

    private ResultData? GetStartResult(Path path)
    {
        if (path.IsRoot)
        {
            return _root;
        }

        var parent = path.Parent;
        var result = GetStartResult(parent);

        if (result is null)
        {
            return null;
        }

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

                case null:
                    return null;
            }
        }

        throw new InvalidOperationException(
            $"The path segment '{parent}' does not exist in the data.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _lock.Dispose();

        while (_memory.TryPop(out var memory))
        {
            memory.Dispose();
        }
    }
}
