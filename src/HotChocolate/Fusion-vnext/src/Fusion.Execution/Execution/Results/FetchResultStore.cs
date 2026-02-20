using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class FetchResultStore : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ISchemaDefinition _schema;
    private readonly IErrorHandler _errorHandler;
    private readonly Operation _operation;
    private readonly ErrorHandlingMode _errorHandlingMode;
    private readonly ulong _includeFlags;
    private readonly ConcurrentStack<IDisposable> _memory = [];
    private CompositeResultDocument _result;
    private ValueCompletion _valueCompletion;
    private List<IError>? _errors;
    private bool _disposed;

    public FetchResultStore(
        ISchemaDefinition schema,
        IErrorHandler errorHandler,
        Operation operation,
        ErrorHandlingMode errorHandlingMode,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operation);

        _schema = schema;
        _errorHandler = errorHandler;
        _operation = operation;
        _errorHandlingMode = errorHandlingMode;
        _includeFlags = includeFlags;

        _result = new CompositeResultDocument(operation, includeFlags);

        _valueCompletion = new ValueCompletion(
            this,
            _schema,
            _errorHandler,
            _errorHandlingMode,
            maxDepth: 32);

        _memory.Push(_result);
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _result = new CompositeResultDocument(_operation, _includeFlags);
        _errors?.Clear();

        _valueCompletion = new ValueCompletion(
            this,
            _schema,
            _errorHandler,
            _errorHandlingMode,
            maxDepth: 32);

        _memory.Push(_result);
    }

    public CompositeResultDocument Result => _result;

    public IReadOnlyList<IError>? Errors => _errors;

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

        var dataElements = ArrayPool<SourceResultElement>.Shared.Rent(results.Length);
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

                var errors = result.Errors;

                if (errors?.RootErrors is { Length: > 0 } rootErrors)
                {
                    _errors ??= [];
                    _errors.AddRange(rootErrors);
                }

                dataElement = GetDataElement(sourcePath, result.Data);
                errorTrie = GetErrorTrie(sourcePath, errors?.Trie);

                result = ref Unsafe.Add(ref result, 1)!;
                dataElement = ref Unsafe.Add(ref dataElement, 1);
                errorTrie = ref Unsafe.Add(ref errorTrie, 1)!;
            }

            return SaveSafe(results, dataElementsSpan, errorTriesSpan, responseNames);
        }
        finally
        {
            ArrayPool<SourceResultElement>.Shared.Return(dataElements);
            ArrayPool<ErrorTrie?>.Shared.Return(errorTries);
        }
    }

    /// <summary>
    /// Adds partial root data to the result document.
    /// </summary>
    /// <param name="document">
    /// The document that contains partial results that need to be merged into the `Data` segment of the result.
    /// </param>
    /// <param name="responseNames">
    /// The names of the root fields the document provides data for.
    /// </param>
    /// <returns>
    /// true if the result was integrated.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="document"/> is null.
    /// </exception>
    public bool AddPartialResults(SourceResultDocument document, ReadOnlySpan<string> responseNames)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(document);

        _lock.EnterWriteLock();

        try
        {
            var partial = document.Root;
            var data = _result.Data;

            return _valueCompletion.BuildResult(
                partial,
                data, errorTrie: null, responseNames: responseNames);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void AddError(IError error)
    {
        _errors ??= [];
        _errors.Add(error);
    }

    public bool AddErrors(IError error, ReadOnlySpan<string> responseNames, params ReadOnlySpan<Path> paths)
    {
        _lock.EnterWriteLock();

        try
        {
            ref var path = ref MemoryMarshal.GetReference(paths);
            ref var end = ref Unsafe.Add(ref path, paths.Length);
            var resultData = _result.Data;

            while (Unsafe.IsAddressLessThan(ref path, ref end))
            {
                if (resultData.IsInvalidated)
                {
                    return false;
                }

                var element = path.IsRoot ? resultData : GetStartObjectResult(path);
                if (element.IsNullOrInvalidated)
                {
                    goto AddErrors_Next;
                }

                var canExecutionContinue =
                    _valueCompletion.BuildErrorResult(
                        element,
                        responseNames,
                        error,
                        path);
                if (!canExecutionContinue)
                {
                    resultData.Invalidate();
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
        ReadOnlySpan<SourceResultElement> dataElements,
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
            var resultData = _result.Data;

            while (Unsafe.IsAddressLessThan(ref result, ref end))
            {
                if (resultData.IsNullOrInvalidated)
                {
                    return false;
                }

                var element = result.Path.IsRoot ? resultData : GetStartObjectResult(result.Path);
                if (element.IsNullOrInvalidated)
                {
                    goto SaveSafe_Next;
                }

                var canExecutionContinue =
                    _valueCompletion.BuildResult(
                        data,
                        element,
                        errorTrie,
                        responseNames);

                if (!canExecutionContinue)
                {
                    resultData.Invalidate();
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
            var current = new List<CompositeResultElement> { _result.Data };
            var next = new List<CompositeResultElement>();

            for (var i = 0; i < selectionSet.Segments.Length; i++)
            {
                var segment = selectionSet.Segments[i];

                foreach (var element in current)
                {
                    if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
                    {
                        if (element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var value)
                            && value.ValueKind is JsonValueKind.String
                            && value.TextEqualsHelper(segment.Name, isPropertyName: false))
                        {
                            next.Add(element);
                        }
                    }
                    else if (segment.Kind is SelectionPathSegmentKind.Field)
                    {
                        if (!element.TryGetProperty(segment.Name, out var value))
                        {
                            continue;
                        }

                        var valueKind = value.ValueKind;

                        if (valueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        {
                            continue;
                        }

                        if (valueKind is JsonValueKind.Array)
                        {
                            next.AddRange(UnrollLists(value));
                            continue;
                        }

                        if (valueKind is JsonValueKind.Object)
                        {
                            next.Add(value);
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
            Dictionary<ObjectValueNode, int>? seen = null;
            List<Path>?[]? additionalPaths = null;
            var nextIndex = 0;

            foreach (var result in current)
            {
                var variables = MapRequirements(
                    result,
                    requestVariables,
                    requiredData,
                    ref buffer);

                if (variables is null)
                {
                    continue;
                }

                variableValueSets ??= new VariableValues[current.Count];

                if (nextIndex > 0)
                {
                    seen ??= new Dictionary<ObjectValueNode, int>(VariableValueComparer.Instance)
                    {
                        [variableValueSets[0].Values] = 0
                    };

                    if (seen.TryGetValue(variables, out var existingIndex))
                    {
                        additionalPaths ??= new List<Path>?[current.Count];
                        (additionalPaths[existingIndex] ??= []).Add(result.Path);
                        continue;
                    }

                    seen[variables] = nextIndex;
                }

                variableValueSets[nextIndex++] = new VariableValues(result.Path, variables);
            }

            if (additionalPaths is not null)
            {
                for (var i = 0; i < nextIndex; i++)
                {
                    if (additionalPaths[i] is { } paths)
                    {
                        variableValueSets![i] = variableValueSets[i] with
                        {
                            AdditionalPaths = [.. paths]
                        };
                    }
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
        CompositeResultElement result,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements,
        ref PooledArrayWriter? buffer)
    {
        var fields = new List<ObjectFieldNode>(forwardedVariables.Count + requirements.Length);
        fields.AddRange(forwardedVariables);

        foreach (var requirement in requirements)
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
        CompositeResultElement result,
        string key,
        IValueSelectionNode path,
        ref PooledArrayWriter? buffer)
    {
        var value = ResultDataMapper.Map(result, path, _schema, ref buffer);
        return value is null ? null : new ObjectFieldNode(key, value);
    }

    private static IEnumerable<CompositeResultElement> UnrollLists(CompositeResultElement list)
    {
        foreach (var element in list.EnumerateArray())
        {
            var elementValueKind = element.ValueKind;

            if (elementValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            if (elementValueKind is JsonValueKind.Array)
            {
                foreach (var nestedElement in UnrollLists(element))
                {
                    yield return nestedElement;
                }
            }
            else
            {
                yield return element;
            }
        }
    }

    public PooledArrayWriter CreateRentedBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new PooledArrayWriter();
        _memory.Push(buffer);
        return buffer;
    }

    private static SourceResultElement GetDataElement(SelectionPath sourcePath, SourceResultElement data)
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

    private CompositeResultElement GetStartObjectResult(Path path)
    {
        var result = GetStartResult(path);
        Debug.Assert(result.ValueKind is JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined);
        return result.ValueKind is JsonValueKind.Object or JsonValueKind.Null ? result : default;
    }

    private CompositeResultElement GetStartResult(Path path)
    {
        if (path.IsRoot)
        {
            return _result.Data;
        }

        var parent = path.Parent;
        var element = GetStartResult(parent);
        var elementKind = element.ValueKind;

        if (elementKind is JsonValueKind.Null)
        {
            return element;
        }

        if (elementKind is JsonValueKind.Object && path is NamePathSegment nameSegment)
        {
            return element.TryGetProperty(nameSegment.Name, out var field) ? field : default;
        }

        if (elementKind is JsonValueKind.Array && path is IndexerPathSegment indexSegment)
        {
            if (element.GetArrayLength() <= indexSegment.Index)
            {
                throw new InvalidOperationException(
                    $"The path segment '{indexSegment}' does not exist in the data.");
            }

            return element[indexSegment.Index];
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
