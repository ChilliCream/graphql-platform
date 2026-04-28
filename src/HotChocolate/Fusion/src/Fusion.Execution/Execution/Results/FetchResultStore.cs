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
    private static readonly ArrayPool<VariableValues> s_variableValuePool = ArrayPool<VariableValues>.Shared;
    private static readonly ArrayPool<object> s_objectPool = ArrayPool<object>.Shared;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<IDisposable> _memory = [];
    private readonly ChunkedArrayWriter _variableWriter = new();
    private readonly JsonWriter _jsonWriter;
    private readonly VariableDedupTable _variableDedupTable;
    private ISchemaDefinition _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private Operation _operation = default!;
    private ErrorHandlingMode _errorHandlingMode;
    private ulong _includeFlags;
    private ulong _deferFlags;
    private CompositeResultElement[] _collectTargetA = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetB = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetCombined = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private PathSegmentLocalPool _pathPool = default!;
    private HashSet<int[]> _seenPaths = new(ReferenceEqualityComparer.Instance);
    private CompositeResultDocument _result = default!;
    private ValueCompletion _valueCompletion = default!;
    private List<IError>? _errors;
    private Dictionary<Path, IError>? _pocketedErrors;
    private bool _disposed;

    internal FetchResultStore()
    {
        _jsonWriter = new JsonWriter(_variableWriter, new JsonWriterOptions { Indented = false });
        _variableDedupTable = new VariableDedupTable(_variableWriter);
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
                data,
                errorTrie: null,
                resultSelectionSet: resultSelectionSet);
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

        var buffer = s_objectPool.Rent(path.Length);
        var segments = buffer.AsSpan(0, path.Length);

        try
        {
            path.CopyTo(segments);

            for (var i = 0; i < segments.Length; i++)
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
        finally
        {
            segments.Clear();
            s_objectPool.Return(buffer);
        }
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

        var element = path.IsRoot ? resultData : GetStartResult(path);
        Debug.Assert(element.ValueKind is JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined);

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

        lock (_lock)
        {
            var elements = CollectTargetElements(selectionSet);

            if (elements.IsEmpty)
            {
                return [];
            }

            return BuildVariableValueSets(elements, requestVariables, requiredData);
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

        lock (_lock)
        {
            var combinedCount = 0;

            foreach (var selectionSet in selectionSets)
            {
                var elements = CollectTargetElements(selectionSet);

                if (!elements.IsEmpty)
                {
                    EnsureCapacity(
                        ref _collectTargetCombined,
                        combinedCount + elements.Length,
                        combinedCount);
                    elements.CopyTo(_collectTargetCombined.AsSpan(combinedCount));
                    combinedCount += elements.Length;
                }
            }

            if (combinedCount == 0)
            {
                return [];
            }

            return BuildVariableValueSets(
                _collectTargetCombined.AsSpan(0, combinedCount),
                requestVariables,
                requiredData);
        }
    }

    internal ImmutableArray<VariableValues> CreateVariableValueSetsFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        HashSet<string> importedKeys,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(importedKeys);
        ArgumentNullException.ThrowIfNull(requestVariables);

        if (requiredData.Length == 0)
        {
            throw new ArgumentException(
                "The required data span must contain at least one requirement.",
                nameof(requiredData));
        }

        if (importedEntries.IsDefaultOrEmpty)
        {
            return [];
        }

        foreach (var requirement in requiredData)
        {
            if (!importedKeys.Contains(requirement.Key))
            {
                throw new InvalidOperationException(
                    "A deferred sub-plan fetch references a requirement that was not imported.");
            }
        }

        lock (_lock)
        {
            return BuildVariableValueSetsFromSnapshot(importedEntries, requestVariables, requiredData);
        }
    }

    // Caller must hold _lock for reading.
    private ReadOnlySpan<CompositeResultElement> CollectTargetElements(SelectionPath selectionSet)
    {
        var current = _collectTargetA;
        var currentCount = 0;
        var next = _collectTargetB;
        var nextCount = 0;

        current[currentCount++] = _result.Data;

        for (var i = 0; i < selectionSet.Length; i++)
        {
            var segment = selectionSet[i];

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];
                    if (element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var value)
                        && value.ValueKind is JsonValueKind.String
                        && value.TextEqualsHelper(segment.Name, isPropertyName: false))
                    {
                        AddToBuffer(ref next, ref nextCount, element);
                    }
                }
            }
            else if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];
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
                        AppendUnrolledLists(value, ref next, ref nextCount);
                        continue;
                    }

                    if (valueKind is JsonValueKind.Object)
                    {
                        AddToBuffer(ref next, ref nextCount, value);
                        continue;
                    }

                    // TODO : Better error
                    throw new NotSupportedException("Must be list or object.");
                }
            }

            (current, next) = (next, current);
            (currentCount, nextCount) = (nextCount, 0);

            if (currentCount == 0)
            {
                // Store potentially grown arrays back.
                _collectTargetA = current;
                _collectTargetB = next;
                return [];
            }
        }

        // Store potentially grown arrays back.
        _collectTargetA = current;
        _collectTargetB = next;
        return current.AsSpan(0, currentCount);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        _variableDedupTable.Initialize(importedEntries.Length);

        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var importedEntry in importedEntries)
        {
            if (importedEntry.IsEmpty)
            {
                continue;
            }

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            for (var i = 0; i < requestVariables.Count; i++)
            {
                var field = requestVariables[i];
                _jsonWriter.WritePropertyName(field.Name.Value);
                WriteValueNode(field.Value);
            }

            if (!TryWriteRequestedRequirementValues(importedEntry.Values, requiredData))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                importedEntry.Path,
                startPosition,
                ref additionalPaths,
                nextIndex,
                out var dedupIndex);

            if (entry is null)
            {
                additionalPaths.AddRange(dedupIndex, importedEntry.AdditionalPaths.AsSpan());
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(importedEntries.Length);
            variableValueSets[nextIndex] = entry.Value;
            additionalPaths.AddRange(nextIndex, importedEntry.AdditionalPaths.AsSpan());
            nextIndex++;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSets(
        ReadOnlySpan<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        _variableDedupTable.Initialize(elements.Length);
        PooledArrayWriter? buffer = null;

        if (requestVariables.Count == 0)
        {
            var fastPathResult = requiredData.Length switch
            {
                1 => BuildVariableValueSetsSingleRequirement(
                    elements,
                    requiredData[0]),

                2 => BuildVariableValueSetsTwoRequirements(
                    elements,
                    requiredData[0],
                    requiredData[1]),

                3 => BuildVariableValueSetsThreeRequirements(
                    elements,
                    requiredData[0],
                    requiredData[1],
                    requiredData[2]),
                _ => default
            };

            if (!fastPathResult.IsDefault)
            {
                if (buffer is not null)
                {
                    lock (_lock)
                    {
                        _memory.Add(buffer);
                    }
                }

                return fastPathResult;
            }
        }

        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            // Write forwarded variables.
            for (var i = 0; i < requestVariables.Count; i++)
            {
                var field = requestVariables[i];
                _jsonWriter.WritePropertyName(field.Name.Value);
                WriteValueNode(field.Value);
            }

            // Write requirement fields.
            var failed = false;

            foreach (var requirement in requiredData)
            {
                _jsonWriter.WritePropertyName(requirement.Key);

                if (!ResultDataMapper.TryMap(result, requirement.Map, _schema, _jsonWriter))
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        if (buffer is not null)
        {
            lock (_lock)
            {
                _memory.Add(buffer);
            }
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirement(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        if (TryGetSimpleRequirementFieldName(requirement.Map, out var fieldName))
        {
            return BuildVariableValueSetsSingleRequirementFastPath(elements, requirement, fieldName);
        }

        return BuildVariableValueSetsSingleRequirementSlowPath(elements, requirement);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement,
        string fieldName)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var isNonNullRequirement = requirement.Type.Kind is SyntaxKind.NonNullType;

        for (var i = 0; i < elements.Length; i++)
        {
            var result = elements[i];

            if (!result.TryGetProperty(fieldName, out var value))
            {
                continue;
            }

            var valueKind = value.ValueKind;

            if (valueKind is JsonValueKind.Undefined)
            {
                continue;
            }

            if (valueKind is JsonValueKind.Null && isNonNullRequirement)
            {
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;

            // Write variable JSON: {"key":rawValue}
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);
            WriteCompositeResultValue(value);
            _jsonWriter.WriteEndObject();

            // we try to create a VariableValues object,
            // if that fails the variables already were created and we move on.
            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);

            if (!ResultDataMapper.TryMap(result, requirement.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        if (TryGetSimpleRequirementFieldName(requirement1.Map, out var fieldName1)
            && TryGetSimpleRequirementFieldName(requirement2.Map, out var fieldName2))
        {
            return BuildVariableValueSetsTwoRequirementsFastPath(
                elements,
                requirement1,
                fieldName1,
                requirement2,
                fieldName2);
        }

        return BuildVariableValueSetsTwoRequirementsSlowPath(
            elements,
            requirement1,
            requirement2);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(result, requirement1.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(result, requirement2.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        if (TryGetSimpleRequirementFieldName(requirement1.Map, out var fieldName1)
            && TryGetSimpleRequirementFieldName(requirement2.Map, out var fieldName2)
            && TryGetSimpleRequirementFieldName(requirement3.Map, out var fieldName3))
        {
            return BuildVariableValueSetsThreeRequirementsFastPath(
                elements,
                requirement1,
                fieldName1,
                requirement2,
                fieldName2,
                requirement3,
                fieldName3);
        }

        return BuildVariableValueSetsThreeRequirementsSlowPath(
            elements,
            requirement1,
            requirement2,
            requirement3);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        OperationRequirement requirement3,
        string fieldName3)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName3, out var value3)
                || value3.ValueKind is JsonValueKind.Undefined
                || (value3.ValueKind is JsonValueKind.Null
                    && requirement3.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WritePropertyName(requirement3.Key);
            WriteCompositeResultValue(value3);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(_variableWriter);
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(result, requirement1.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(result, requirement2.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement3.Key);

            if (!ResultDataMapper.TryMap(result, requirement3.Map, _schema, _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result.CompactPath, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        _variableDedupTable.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private static bool TryGetSimpleRequirementFieldName(
        IValueSelectionNode map,
        [NotNullWhen(true)] out string? fieldName)
    {
        if (map is PathNode
            {
                TypeName: null,
                PathSegment:
                {
                    TypeName: null,
                    PathSegment: null
                } pathSegment
            })
        {
            fieldName = pathSegment.FieldName.Value;
            return true;
        }

        fieldName = null;
        return false;
    }

    private VariableValues? TryCreateVariableValues(
        CompactPath path,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
        => TryCreateVariableValues(path, startPosition, ref additionalPaths, nextIndex, out _);

    private VariableValues? TryCreateVariableValues(
        CompactPath path,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex,
        out int dedupIndex)
    {
        var length = _variableWriter.Position - startPosition;
        var hash = _variableWriter.GetHashCode(startPosition, length);

        // we we already track the sae variables we will track them as additional paths
        // on the existing index.
        //
        // this allows us to fetch once and then insert the data at different locations.
        if (_variableDedupTable.TryGet(hash, startPosition, length, out var existingIndex))
        {
            dedupIndex = existingIndex;
            additionalPaths.Add(existingIndex, path);
            _variableWriter.ResetTo(startPosition);
            return null;
        }

        dedupIndex = nextIndex;
        _variableDedupTable.Add(hash, nextIndex, startPosition, length);
        return new VariableValues(path, JsonSegment.Create(_variableWriter, startPosition, length));
    }

    private bool TryWriteRequestedRequirementValues(
        JsonSegment values,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        if (values.IsEmpty)
        {
            return false;
        }

        var sequence = values.AsSequence();

        foreach (var requirement in requiredData)
        {
            if (!TryWriteRequirementValue(sequence, requirement.Key))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryWriteRequirementValue(ReadOnlySequence<byte> values, string key)
    {
        var reader = new Utf8JsonReader(values);

        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
        {
            return false;
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return false;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                return false;
            }

            var matches = reader.ValueTextEquals(key);

            if (!reader.Read())
            {
                return false;
            }

            var start = reader.TokenStartIndex;
            reader.Skip();
            var length = reader.BytesConsumed - start;

            if (matches)
            {
                _jsonWriter.WritePropertyName(key);
                WriteRawJsonValue(values.Slice(start, length));
                return true;
            }
        }

        return false;
    }

    private void WriteRawJsonValue(ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            _jsonWriter.WriteRawValue(value.FirstSpan);
            return;
        }

        _jsonWriter.WriteRawValue(value.ToArray());
    }

    private void WriteValueNode(IValueNode value)
    {
        switch (value)
        {
            case NullValueNode:
                _jsonWriter.WriteNullValue();
                break;

            case StringValueNode sv:
                _jsonWriter.WriteStringValue(sv.Value);
                break;

            case IntValueNode iv:
                WriteRawAscii(iv.Value);
                break;

            case FloatValueNode fv:
                WriteRawAscii(fv.Value);
                break;

            case BooleanValueNode bv:
                _jsonWriter.WriteBooleanValue(bv.Value);
                break;

            case EnumValueNode ev:
                _jsonWriter.WriteStringValue(ev.Value);
                break;

            case ObjectValueNode ov:
                _jsonWriter.WriteStartObject();
                foreach (var field in ov.Fields)
                {
                    _jsonWriter.WritePropertyName(field.Name.Value);
                    WriteValueNode(field.Value);
                }
                _jsonWriter.WriteEndObject();
                break;

            case ListValueNode lv:
                _jsonWriter.WriteStartArray();
                foreach (var item in lv.Items)
                {
                    WriteValueNode(item);
                }
                _jsonWriter.WriteEndArray();
                break;

            default:
                _jsonWriter.WriteNullValue();
                break;
        }
    }

    private void WriteRawAscii(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        System.Text.Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        _jsonWriter.WriteRawValue(buffer);
    }

    private void WriteCompositeResultValue(CompositeResultElement value)
        => value.WriteTo(_jsonWriter);

    internal VariableValues CreateVariableValueSets(
        CompactPath path,
        IReadOnlyList<ObjectFieldNode> fields)
    {
        _jsonWriter.Reset(_variableWriter);
        var startPosition = _variableWriter.Position;
        _jsonWriter.WriteStartObject();

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            _jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(field.Value);
        }

        _jsonWriter.WriteEndObject();
        var length = _variableWriter.Position - startPosition;
        return new VariableValues(path, JsonSegment.Create(_variableWriter, startPosition, length));
    }

    /// <summary>
    /// Copies variable value sets produced by another <see cref="FetchResultStore"/>
    /// into this store's writer and path pool, then initializes the child-store
    /// containers needed to reach imported list-anchor paths. Used by deferred
    /// sub-plans: plan-scope requirement values are resolved once against the
    /// parent store at sub-plan creation time, materialized here, and consumed
    /// without any dependency on the parent store's lifetime.
    /// </summary>
    internal ImmutableArray<VariableValues> ImportVariableValues(
        ImmutableArray<VariableValues> source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (source.IsDefaultOrEmpty)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<VariableValues>(source.Length);

        ImmutableArray<VariableValues> imported;

        lock (_lock)
        {
            foreach (var entry in source)
            {
                builder.Add(ImportVariableValuesEntry(entry));
            }

            imported = builder.MoveToImmutable();
        }

        InitializeTargetPaths(imported);

        return imported;
    }

    private VariableValues ImportVariableValuesEntry(VariableValues source)
    {
        var path = ImportPath(source.Path);
        var values = ImportJsonSegment(source.Values);
        var additionalPaths = ImportAdditionalPaths(source.AdditionalPaths);

        return new VariableValues(path, values)
        {
            AdditionalPaths = additionalPaths
        };
    }

    private JsonSegment ImportJsonSegment(JsonSegment source)
    {
        if (source.IsEmpty)
        {
            return JsonSegment.Empty;
        }

        var startPosition = _variableWriter.Position;
        foreach (var memory in source.AsSequence())
        {
            var span = _variableWriter.GetSpan(memory.Length);
            memory.Span.CopyTo(span);
            _variableWriter.Advance(memory.Length);
        }

        var length = _variableWriter.Position - startPosition;
        return JsonSegment.Create(_variableWriter, startPosition, length);
    }

    private static CompactPath ImportPath(CompactPath source)
    {
        if (source.IsRoot)
        {
            return CompactPath.Root;
        }

        var segments = source.Segments;
        var copy = new int[segments.Length + 1];
        copy[0] = segments.Length;
        segments.CopyTo(copy.AsSpan(1));
        return new CompactPath(copy);
    }

    private static CompactPathSegment ImportAdditionalPaths(CompactPathSegment source)
    {
        if (source.IsDefaultOrEmpty)
        {
            return default;
        }

        var paths = source.AsSpan();
        var copy = new CompactPath[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            copy[i] = ImportPath(paths[i]);
        }

        return new CompactPathSegment(copy, 0, copy.Length);
    }

    private static void AppendUnrolledLists(
        CompositeResultElement list,
        ref CompositeResultElement[] destination,
        ref int destinationCount)
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
                AppendUnrolledLists(element, ref destination, ref destinationCount);
            }
            else
            {
                AddToBuffer(ref destination, ref destinationCount, element);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddToBuffer(
        ref CompositeResultElement[] buffer,
        ref int count,
        CompositeResultElement value)
    {
        if (count == buffer.Length)
        {
            GrowBuffer(ref buffer, count);
        }

        buffer[count++] = value;
    }

    private static void GrowBuffer(
        ref CompositeResultElement[] buffer,
        int count)
    {
        var newBuffer = ArrayPool<CompositeResultElement>.Shared.Rent(buffer.Length * 2);
        buffer.AsSpan(0, count).CopyTo(newBuffer);
        ArrayPool<CompositeResultElement>.Shared.Return(buffer, clearArray: true);
        buffer = newBuffer;
    }

    private static void EnsureCapacity(
        ref CompositeResultElement[] buffer,
        int required,
        int count)
    {
        if (required > buffer.Length)
        {
            var newBuffer = ArrayPool<CompositeResultElement>.Shared.Rent(required);
            buffer.AsSpan(0, count).CopyTo(newBuffer);
            ArrayPool<CompositeResultElement>.Shared.Return(buffer, clearArray: true);
            buffer = newBuffer;
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
                if (element.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"The path segment '{segment}' does not exist in the data.");
                }

                element = element.GetPropertyBySelectionId(segment);
            }
            else
            {
                var index = ~segment;
                element = element[index];
            }
        }

        return element;
    }

    private void InitializeTargetPaths(ImmutableArray<VariableValues> importedValues)
    {
        if (importedValues.IsDefaultOrEmpty)
        {
            return;
        }

        lock (_lock)
        {
            // Collect the maximum array index required for each distinct list container
            // slot. Key = cursor index of the container element (unique in the meta-db).
            // In practice there is at most one distinct list container per sub-plan
            // because all requirements must originate from a single anchor.
            Dictionary<int, (CompositeResultElement Container, int MaxIndex)>? containers = null;

            foreach (var entry in importedValues)
            {
                TrackListContainer(entry.Path, ref containers);
                foreach (var additional in entry.AdditionalPaths.AsSpan())
                {
                    TrackListContainer(additional, ref containers);
                }
            }

            if (containers is null)
            {
                return;
            }

            foreach (var (_, (container, maxIndex)) in containers)
            {
                if (container.ValueKind is JsonValueKind.Undefined)
                {
                    container.SetArrayValue(maxIndex + 1);
                }
                else if (container.ValueKind is JsonValueKind.Array)
                {
                    if (container.GetArrayLength() <= maxIndex)
                    {
                        throw new InvalidOperationException(
                            $"The target path list container is shorter than required for index {maxIndex}.");
                    }
                }
                else if (container.ValueKind is not JsonValueKind.Null)
                {
                    throw new InvalidOperationException(
                        "The target path list container does not exist in the data.");
                }
            }
        }
    }

    private void TrackListContainer(
        CompactPath path,
        ref Dictionary<int, (CompositeResultElement Container, int MaxIndex)>? containers)
    {
        if (path.IsRoot)
        {
            return;
        }

        var element = _result.Data;
        var segments = path.Segments;

        for (var i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];

            if (element.ValueKind is JsonValueKind.Null)
            {
                return;
            }

            if (seg >= 0)
            {
                var hasListAnchor = HasListAnchor(segments, i + 1);

                if (element.ValueKind is JsonValueKind.Undefined)
                {
                    if (!hasListAnchor)
                    {
                        return;
                    }

                    InitializeIntermediateObject(element);
                }

                if (element.ValueKind is not JsonValueKind.Object)
                {
                    if (!hasListAnchor)
                    {
                        return;
                    }

                    throw new InvalidOperationException(
                        $"The path segment '{seg}' does not exist in the data.");
                }

                element = element.GetPropertyBySelectionId(seg);
            }
            else
            {
                // Negative segment encodes a list index as ~index.
                var index = ~seg;
                var cursorKey = element.Cursor.Index;

                containers ??= new Dictionary<int, (CompositeResultElement, int)>();

                if (containers.TryGetValue(cursorKey, out var existing))
                {
                    if (index > existing.MaxIndex)
                    {
                        containers[cursorKey] = (existing.Container, index);
                    }
                }
                else
                {
                    containers[cursorKey] = (element, index);
                }

                // Only process the outermost list anchor; stop here.
                return;
            }
        }
    }

    private static bool HasListAnchor(ReadOnlySpan<int> segments, int start)
    {
        for (var i = start; i < segments.Length; i++)
        {
            if (segments[i] < 0)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeIntermediateObject(CompositeResultElement element)
    {
        var selection = element.Selection
            ?? throw new InvalidOperationException(
                "Cannot initialize an intermediate target object without selection metadata.");

        if (selection.Type.NamedType() is not IObjectTypeDefinition objectType)
        {
            throw new InvalidOperationException(
                "Cannot initialize an intermediate target object for an abstract selection.");
        }

        var selectionSet = selection.DeclaringSelectionSet.DeclaringOperation
            .GetSelectionSet(selection, objectType);

        element.SetObjectValue(selectionSet);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetA, clearArray: true);
        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetB, clearArray: true);
        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetCombined, clearArray: true);

        foreach (var memory in _memory)
        {
            memory.Dispose();
        }

        _memory.Clear();

        _variableWriter.Dispose();
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

    private static ImmutableArray<VariableValues> FinalizeVariableValueSets(
        VariableValues[]? variableValueSets,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
    {
        if (variableValueSets is null || nextIndex == 0)
        {
            if (variableValueSets is not null)
            {
                s_variableValuePool.Return(variableValueSets, clearArray: true);
            }

            additionalPaths.Dispose();
            return [];
        }

        additionalPaths.ApplyTo(variableValueSets, nextIndex);
        additionalPaths.Dispose();

        var span = variableValueSets.AsSpan(0, nextIndex);
        var result = span.ToArray();
        span.Clear();
        s_variableValuePool.Return(variableValueSets);

        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    private sealed class VariableDedupTable(ChunkedArrayWriter writer) : IDisposable
    {
        private const int DefaultBucketSize = 4;
        private const int DefaultBucketCount = 16;

        private readonly ChunkedArrayWriter _writer = writer;
        private Entry[] _table = ArrayPool<Entry>.Shared.Rent(DefaultBucketCount * DefaultBucketSize);
        private int _bucketCount = DefaultBucketCount;
        private readonly int _bucketSize = DefaultBucketSize;

        public void Initialize(int capacity)
        {
            _bucketCount = NextPowerOfTwo(Math.Max(capacity, DefaultBucketCount));
            var totalSize = _bucketCount * _bucketSize;

            if (_table.Length < totalSize)
            {
                ArrayPool<Entry>.Shared.Return(_table);
                _table = ArrayPool<Entry>.Shared.Rent(totalSize);
            }

            _table.AsSpan(0, totalSize).Clear();
        }

        public bool TryGet(
            int hash,
            int location,
            int length,
            out int existingIndex)
        {
            var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
            var start = bucket * _bucketSize;
            var end = start + _bucketSize;

            for (var s = start; s < end; s++)
            {
                ref var entry = ref _table[s];

                if (entry.Index == 0)
                {
                    existingIndex = -1;
                    return false;
                }

                if (entry.Hash == hash
                    && entry.Length == length
                    && _writer.SequenceEqual(entry.Location, location, length))
                {
                    existingIndex = entry.Index - 1;
                    return true;
                }
            }

            existingIndex = -1;
            return false;
        }

        public void Add(int hash, int index, int location, int length)
        {
            var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
            var start = bucket * _bucketSize;
            var end = start + _bucketSize;

            for (var s = start; s < end; s++)
            {
                ref var entry = ref _table[s];

                if (entry.Index == 0)
                {
                    entry.Hash = hash;
                    entry.Index = index + 1;
                    entry.Location = location;
                    entry.Length = length;
                    return;
                }
            }

            Grow();
            Add(hash, index, location, length);
        }

        public void Clear()
            => _table.AsSpan(0, _bucketCount * _bucketSize).Clear();

        public void Dispose()
        {
            ArrayPool<Entry>.Shared.Return(_table);
            _table = [];
        }

        private void Grow()
        {
            var oldTable = _table;
            var oldTotal = _bucketCount * _bucketSize;

            _bucketCount *= 2;
            var newTotal = _bucketCount * _bucketSize;
            _table = ArrayPool<Entry>.Shared.Rent(newTotal);
            _table.AsSpan(0, newTotal).Clear();

            for (var i = 0; i < oldTotal; i++)
            {
                var entry = oldTable[i];

                if (entry.Index != 0)
                {
                    Add(entry.Hash, entry.Index - 1, entry.Location, entry.Length);
                }
            }

            ArrayPool<Entry>.Shared.Return(oldTable);
        }

        private static int NextPowerOfTwo(int n)
        {
            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            return n + 1;
        }

        private struct Entry
        {
            public int Hash;
            public int Index;    // 1-based (0 = empty)
            public int Location;
            public int Length;
        }
    }
}
