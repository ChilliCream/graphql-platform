using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore : IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<IDisposable> _memory = [];
    private readonly VariableBuilderPool _builderPool = new();
    private ISchemaDefinition _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private Operation _operation = default!;
    private ErrorHandlingMode _errorHandlingMode;
    private ulong _includeFlags;
    private PathSegmentLocalPool _pathPool = default!;
    private HashSet<int[]> _seenPaths = new(ReferenceEqualityComparer.Instance);
    private CompositeResultDocument _result = default!;
    private ValueCompletion _valueCompletion = default!;
    private List<IError>? _errors;
    private Dictionary<Path, IError>? _pocketedErrors;
    private bool _disposed;

    internal FetchResultStore()
    {
    }

    public CompositeResultDocument Result => _result;

    public IReadOnlyList<IError>? Errors => _errors;

    public List<IDisposable> MemoryOwners => _memory;

    public bool AddPartialResult(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ResultSelectionSet resultSelectionSet,
        bool containsErrors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sourcePath);

        return containsErrors
            ? AddSinglePartialResult(sourcePath, result, resultSelectionSet)
            : AddSinglePartialResultNoErrors(sourcePath, result, resultSelectionSet);
    }

    public bool AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ResultSelectionSet resultSelectionSet,
        bool containsErrors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(sourcePath);

        if (results.Length == 0)
        {
            throw new ArgumentException(
                "The results span must contain at least one result.",
                nameof(results));
        }

        if (!containsErrors)
        {
            return results.Length == 1
                ? AddSinglePartialResultNoErrors(sourcePath, results[0], resultSelectionSet)
                : AddPartialResultsNoErrors(sourcePath, results, resultSelectionSet);
        }

        if (results.Length == 1)
        {
            return AddSinglePartialResult(sourcePath, results[0], resultSelectionSet);
        }

        var dataElements = ArrayPool<SourceResultElement>.Shared.Rent(results.Length);
        var errorTries = ArrayPool<ErrorTrie?>.Shared.Rent(results.Length);
        var dataElementsSpan = dataElements.AsSpan(0, results.Length);
        var errorTriesSpan = errorTries.AsSpan(0, results.Length);
        List<IError>? rootErrors = null;

        try
        {
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var errors = result.Errors;

                if (errors?.RootErrors is { Length: > 0 } rootErrorsFromResult)
                {
                    rootErrors ??= [];
                    rootErrors.AddRange(rootErrorsFromResult);
                }

                dataElementsSpan[i] = GetDataElement(sourcePath, result.Data);
                errorTriesSpan[i] = GetErrorTrie(sourcePath, errors?.Trie);
            }

            lock (_lock)
            {
                try
                {
                    if (rootErrors is not null)
                    {
                        _errors ??= [];
                        _errors.AddRange(rootErrors);
                    }

                    var resultData = _result.Data;

                    for (var i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        _memory.Add(result);

                        if (!SaveSafeResult(
                                resultData,
                                result.Path,
                                result.AdditionalPaths.AsSpan(),
                                dataElementsSpan[i],
                                errorTriesSpan[i],
                                resultSelectionSet))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                finally
                {
                    ReturnPathSegments(results);
                }
            }
        }
        finally
        {
            dataElementsSpan.Clear();
            errorTriesSpan.Clear();
            ArrayPool<SourceResultElement>.Shared.Return(dataElements);
            ArrayPool<ErrorTrie?>.Shared.Return(errorTries);
        }
    }

    private bool AddPartialResultsNoErrors(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ResultSelectionSet resultSelectionSet)
    {
        var dataElements = ArrayPool<SourceResultElement>.Shared.Rent(results.Length);
        var dataElementsSpan = dataElements.AsSpan(0, results.Length);

        try
        {
            for (var i = 0; i < results.Length; i++)
            {
                dataElementsSpan[i] = GetDataElement(sourcePath, results[i].Data);
            }

            lock (_lock)
            {
                try
                {
                    var resultData = _result.Data;

                    for (var i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        _memory.Add(result);

                        if (!SaveSafeResult(
                                resultData,
                                result.Path,
                                result.AdditionalPaths.AsSpan(),
                                dataElementsSpan[i],
                                errorTrie: null,
                                resultSelectionSet))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                finally
                {
                    ReturnPathSegments(results);
                }
            }
        }
        finally
        {
            dataElementsSpan.Clear();
            ArrayPool<SourceResultElement>.Shared.Return(dataElements);
        }
    }

    private bool AddSinglePartialResult(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ResultSelectionSet resultSelectionSet)
    {
        var errors = result.Errors;
        var dataElement = GetDataElement(sourcePath, result.Data);
        var errorTrie = GetErrorTrie(sourcePath, errors?.Trie);

        lock (_lock)
        {
            _memory.Add(result);

            try
            {
                if (errors?.RootErrors is { Length: > 0 } rootErrors)
                {
                    _errors ??= [];
                    _errors.AddRange(rootErrors);
                }

                return SaveSafeResult(
                    _result.Data,
                    result.Path,
                    result.AdditionalPaths.AsSpan(),
                    dataElement,
                    errorTrie,
                    resultSelectionSet);
            }
            finally
            {
                ReturnPathSegments(result);
            }
        }
    }

    private bool AddSinglePartialResultNoErrors(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ResultSelectionSet resultSelectionSet)
    {
        var dataElement = GetDataElement(sourcePath, result.Data);

        lock (_lock)
        {
            _memory.Add(result);

            try
            {
                return SaveSafeResult(
                    _result.Data,
                    result.Path,
                    result.AdditionalPaths.AsSpan(),
                    dataElement,
                    errorTrie: null,
                    resultSelectionSet);
            }
            finally
            {
                ReturnPathSegments(result);
            }
        }
    }

    /// <summary>
    /// Adds partial root data to the result document.
    /// </summary>
    /// <param name="document">
    /// The document that contains partial results that need to be merged into the `Data` segment of the result.
    /// </param>
    /// <param name="resultSelectionSet">
    /// The root selection set the document provides data for.
    /// </param>
    /// <returns>
    /// true if the result was integrated.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="document"/> is null.
    /// </exception>
    public bool AddPartialResults(SourceResultDocument document, ResultSelectionSet resultSelectionSet)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(document);

        lock (_lock)
        {
            var partial = document.Root;
            var data = _result.Data;

            return _valueCompletion.BuildResult(
                partial,
                data, errorTrie: null, resultSelectionSet: resultSelectionSet);
        }
    }

    public void AddError(IError error)
    {
        _errors ??= [];
        _errors.Add(error);
    }

    public bool AddErrors(
        IError error,
        ResultSelectionSet resultSelectionSet,
        params ReadOnlySpan<Path> paths)
    {
        lock (_lock)
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
                        resultSelectionSet,
                        error,
                        element.CompactPath);
                if (!canExecutionContinue)
                {
                    resultData.Invalidate();
                    return false;
                }

AddErrors_Next:
                path = ref Unsafe.Add(ref path, 1)!;
            }
        }

        return true;
    }

    public bool AddErrors(
        IError error,
        ResultSelectionSet resultSelectionSet,
        ReadOnlySpan<CompactPath> paths)
    {
        lock (_lock)
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
                        resultSelectionSet,
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

        return true;
    }

    internal void PocketError(Path path, IError error)
    {
        _pocketedErrors ??= [];
        _pocketedErrors.TryAdd(path, error);
    }

    internal bool HasPocketedErrors
        => _pocketedErrors?.Count > 0;

    internal List<KeyValuePair<Path, IError>> GetPocketedErrorsSnapshot()
        => _pocketedErrors is { Count: > 0 } errors
            ? [.. errors]
            : [];

    internal bool RemovePocketedError(Path path)
        => _pocketedErrors?.Remove(path) ?? false;

    internal void RemovePocketedErrorsInSubtree(Path path)
    {
        if (_pocketedErrors is not { Count: > 0 })
        {
            return;
        }

        List<Path>? pathsToRemove = null;

        foreach (var errorPath in _pocketedErrors.Keys)
        {
            if (PathUtilities.IsPathInSubtree(errorPath, path, includeSelf: true))
            {
                pathsToRemove ??= [];
                pathsToRemove.Add(errorPath);
            }
        }

        if (pathsToRemove is null)
        {
            return;
        }

        foreach (var pathToRemove in pathsToRemove)
        {
            _pocketedErrors.Remove(pathToRemove);
        }
    }

    internal bool TryGetResult(Path path, out CompositeResultElement element)
    {
        element = _result.Data;

        if (path.IsRoot)
        {
            return true;
        }

        var segments = path.ToList();

        for (var i = 0; i < segments.Count; i++)
        {
            switch (segments[i])
            {
                case string fieldName:
                    if (element.ValueKind is not JsonValueKind.Object
                        || !element.TryGetProperty(fieldName, out element))
                    {
                        return false;
                    }

                    break;

                case int index:
                    if (element.ValueKind is not JsonValueKind.Array
                        || index < 0
                        || element.GetArrayLength() <= index)
                    {
                        return false;
                    }

                    element = element[index];
                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    public void FinalizePocketedErrors()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _valueCompletion.FinalizePocketedErrors(_result.Data);
        }
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        CompactPath path,
        ReadOnlySpan<CompactPath> additionalPaths,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ResultSelectionSet resultSelectionSet)
    {
        if (!SaveSafeResult(resultData, path, dataElement, errorTrie, resultSelectionSet))
        {
            return false;
        }

        switch (additionalPaths.Length)
        {
            case 0:
                return true;

            case 1:
                return SaveSafeResult(resultData, additionalPaths[0], dataElement, errorTrie, resultSelectionSet);

            case 2:
                return SaveSafeResult(resultData, additionalPaths[0], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[1], dataElement, errorTrie, resultSelectionSet);

            case 3:
                return SaveSafeResult(resultData, additionalPaths[0], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[1], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[2], dataElement, errorTrie, resultSelectionSet);

            case 4:
                return SaveSafeResult(resultData, additionalPaths[0], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[1], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[2], dataElement, errorTrie, resultSelectionSet)
                    && SaveSafeResult(resultData, additionalPaths[3], dataElement, errorTrie, resultSelectionSet);

            default:
                for (var i = 0; i < additionalPaths.Length; i++)
                {
                    if (!SaveSafeResult(resultData, additionalPaths[i], dataElement, errorTrie, resultSelectionSet))
                    {
                        return false;
                    }
                }

                return true;
        }
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        CompactPath path,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ResultSelectionSet resultSelectionSet)
    {
        if (resultData.IsNullOrInvalidated)
        {
            return false;
        }

        var element = path.IsRoot ? resultData : GetStartObjectResult(path);
        if (element.IsNullOrInvalidated)
        {
            return true;
        }

        var canExecutionContinue =
            _valueCompletion.BuildResult(
                dataElement,
                element,
                errorTrie,
                resultSelectionSet);

        if (canExecutionContinue)
        {
            return true;
        }

        resultData.Invalidate();
        return false;
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

        var builder = _builderPool.Rent();

        try
        {
            return builder.Build(_schema, _result.Data, selectionSet, requestVariables, requiredData);
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    /// <summary>
    /// Creates a deduplicated set of variable values across multiple target selection paths.
    /// Elements from all targets are combined and deduplication is applied globally, so that
    /// entities at different target locations that produce identical variable values are merged
    /// into a single <see cref="VariableValues"/> entry via <see cref="VariableValues.AdditionalPaths"/>.
    /// </summary>
    public ImmutableArray<VariableValues> CreateVariableValueSets(
        ReadOnlySpan<SelectionPath> selectionSets,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requestVariables);

        if (requiredData.Length == 0)
        {
            throw new ArgumentException(
                "The required data span must contain at least one requirement.",
                nameof(requiredData));
        }

        var builder = _builderPool.Rent();

        try
        {
            return builder.Build(_schema, _result.Data, selectionSets, requestVariables, requiredData);
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    internal VariableValues CreateVariableValueSets(
        CompactPath path,
        IReadOnlyList<ObjectFieldNode> fields)
    {
        var builder = _builderPool.Rent();

        try
        {
            return builder.Build(path, fields);
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    public PooledArrayWriter CreateRentedBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new PooledArrayWriter();

        lock (_lock)
        {
            _memory.Add(buffer);
        }

        return buffer;
    }

    private static SourceResultElement GetDataElement(SelectionPath sourcePath, SourceResultElement data)
    {
        if (sourcePath.IsRoot)
        {
            return data;
        }

        var current = data;

        for (var i = 0; i < sourcePath.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            var segment = sourcePath[i];

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
                            || typeNameProperty.ValueKind != JsonValueKind.String
                            || !typeNameProperty.TextEqualsHelper(
                                segment.Name,
                                isPropertyName: false))
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

        for (var i = 0; i < sourcePath.Length; i++)
        {
            var segment = sourcePath[i];

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

    private CompositeResultElement GetStartObjectResult(CompactPath path)
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

    private CompositeResultElement GetStartResult(CompactPath path)
    {
        var element = _result.Data;

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            if (element.ValueKind is JsonValueKind.Null)
            {
                return element;
            }

            if (segment >= 0)
            {
                element = element.GetPropertyBySelectionId(segment);
            }
            else
            {
                var index = ~segment;

                if (element.GetArrayLength() <= index)
                {
                    throw new InvalidOperationException(
                        $"The path segment '[{index}]' does not exist in the data.");
                }

                element = element[index];
            }
        }

        return element;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _builderPool.Dispose();

        foreach (var memory in _memory)
        {
            memory.Dispose();
        }

        _memory.Clear();

        _pathPool?.Dispose();
    }

    private void ReturnPathSegments(ReadOnlySpan<SourceSchemaResult> results)
    {
        for (var i = 0; i < results.Length; i++)
        {
            ReturnPathSegments(results[i], _seenPaths);
        }

        _seenPaths.Clear();
    }

    private void ReturnPathSegments(SourceSchemaResult result)
    {
        ReturnPathSegments(result, _seenPaths);
        _seenPaths.Clear();
    }

    private void ReturnPathSegments(SourceSchemaResult result, HashSet<int[]> seen)
    {
        ReturnPathSegments(result.Path, seen);

        foreach (var additionalPath in result.AdditionalPaths)
        {
            ReturnPathSegments(additionalPath, seen);
        }
    }

    private void ReturnPathSegments(CompactPath path, HashSet<int[]> seen)
    {
        var array = path.UnsafeGetBackingArray();

        if (array is not null && seen.Add(array))
        {
            _pathPool.Return(array);
        }
    }
}
