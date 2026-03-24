using System.Buffers;
using System.Collections.Concurrent;
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

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore : IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ConcurrentStack<IDisposable> _memory = [];
    private ISchemaDefinition _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private Operation _operation = default!;
    private ErrorHandlingMode _errorHandlingMode;
    private ulong _includeFlags;
    private CompositeResultElement[] _collectTargetA = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetB = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetCombined = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private PathSegmentLocalPool _pathPool = default!;
    private HashSet<int[]> _seenPaths = new(ReferenceEqualityComparer.Instance);
    private Dictionary<string, int> _seenStrings = new(StringComparer.Ordinal);
    private Dictionary<IValueNode, int> _seenValueNodes = new(SingleValueNodeComparer.Instance);
    private Dictionary<TwoValueNodeTuple, int> _seenTwoValueTuples = new(TwoValueNodeTupleComparer.Instance);
    private Dictionary<ThreeValueNodeTuple, int> _seenThreeValueTuples = new(ThreeValueNodeTupleComparer.Instance);
    private CompositeResultDocument _result = default!;
    private ValueCompletion _valueCompletion = default!;
    private List<IError>? _errors;
    private Dictionary<Path, IError>? _pocketedErrors;
    private bool _disposed;

    public CompositeResultDocument Result => _result;

    public IReadOnlyList<IError>? Errors => _errors;

    public ConcurrentStack<IDisposable> MemoryOwners => _memory;

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

                // we need to track the result objects as they use rented memory.
                _memory.Push(result);

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
                var result = results[i];
                _memory.Push(result);
                dataElementsSpan[i] = GetDataElement(sourcePath, result.Data);
            }

            lock (_lock)
            {
                try
                {
                    var resultData = _result.Data;

                    for (var i = 0; i < results.Length; i++)
                    {
                        var result = results[i];

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
        _memory.Push(result);

        var errors = result.Errors;
        var dataElement = GetDataElement(sourcePath, result.Data);
        var errorTrie = GetErrorTrie(sourcePath, errors?.Trie);

        lock (_lock)
        {
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
        _memory.Push(result);
        var dataElement = GetDataElement(sourcePath, result.Data);

        lock (_lock)
        {
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

        for (var i = 0; i < additionalPaths.Length; i++)
        {
            if (!SaveSafeResult(resultData, additionalPaths[i], dataElement, errorTrie, resultSelectionSet))
            {
                return false;
            }
        }

        return true;
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

    private ImmutableArray<VariableValues> BuildVariableValueSets(
        ReadOnlySpan<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        PooledArrayWriter? buffer = null;

        if (requestVariables.Count == 0)
        {
            var fastPathResult = requiredData.Length switch
            {
                1 => BuildVariableValueSetsSingleRequirement(
                    elements,
                    requiredData[0],
                    ref buffer),

                2 => BuildVariableValueSetsTwoRequirements(
                    elements,
                    requiredData[0],
                    requiredData[1],
                    ref buffer),

                3 => BuildVariableValueSetsThreeRequirements(
                    elements,
                    requiredData[0],
                    requiredData[1],
                    requiredData[2],
                    ref buffer),
                _ => default
            };

            if (!fastPathResult.IsDefault)
            {
                if (buffer is not null)
                {
                    _memory.Push(buffer);
                }

                return fastPathResult;
            }
        }

        VariableValues[]? variableValueSets = null;
        Dictionary<ObjectValueNode, int>? seen = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;

        foreach (var result in elements)
        {
            var variables = MapRequirements(result, requestVariables, requiredData, ref buffer);

            if (variables is null)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<ObjectValueNode, int>(elements.Length, VariableValueComparer.Instance)
                {
                    [variableValueSets[0].Values] = 0
                };

                if (seen.TryGetValue(variables, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                seen[variables] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(result.CompactPath, variables);
        }

        if (buffer is not null)
        {
            _memory.Push(buffer);
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirement(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement,
        ref PooledArrayWriter? buffer)
    {
        if (TryGetSimpleRequirementFieldName(requirement.Map, out var fieldName))
        {
            return BuildVariableValueSetsSingleRequirementFastPath(
                elements,
                requirement,
                fieldName,
                ref buffer);
        }

        return BuildVariableValueSetsSingleRequirementSlowPath(elements, requirement, ref buffer);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement,
        string fieldName,
        ref PooledArrayWriter? buffer)
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

            variableValueSets ??= new VariableValues[elements.Length];
            IValueNode mappedValue;

            if (valueKind is JsonValueKind.String)
            {
                var stringValue = value.AssertString();

                if (_seenStrings.TryGetValue(stringValue, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                mappedValue = ResultDataMapper.GetStringValueNode(stringValue);
                _seenStrings[stringValue] = nextIndex;
            }
            else
            {
                mappedValue = ResultDataMapper.MapLeafValue(value, ref buffer);

                if (_seenValueNodes.TryGetValue(mappedValue, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenValueNodes[mappedValue] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([
                    new ObjectFieldNode(
                        requirement.Key,
                        mappedValue)
                ]));
        }

        _seenStrings.Clear();
        _seenValueNodes.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var seeded = false;

        foreach (var result in elements)
        {
            var value = ResultDataMapper.Map(result, requirement.Map, _schema, ref buffer);

            if (value is null)
            {
                continue;
            }

            if (value.Kind == SyntaxKind.NullValue && requirement.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];

            if (nextIndex > 0)
            {
                if (!seeded)
                {
                    _seenValueNodes[variableValueSets[0].Values.Fields[0].Value] = 0;
                    seeded = true;
                }

                if (_seenValueNodes.TryGetValue(value, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenValueNodes[value] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([new ObjectFieldNode(requirement.Key, value)]));
        }

        _seenValueNodes.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        ref PooledArrayWriter? buffer)
    {
        if (TryGetSimpleRequirementFieldName(requirement1.Map, out var fieldName1)
            && TryGetSimpleRequirementFieldName(requirement2.Map, out var fieldName2))
        {
            return BuildVariableValueSetsTwoRequirementsFastPath(
                elements,
                requirement1,
                fieldName1,
                requirement2,
                fieldName2,
                ref buffer);
        }

        return BuildVariableValueSetsTwoRequirementsSlowPath(
            elements,
            requirement1,
            requirement2,
            ref buffer);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var seeded = false;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            var mappedValue1 = MapRequirementLeafValue(value1, ref buffer);
            var mappedValue2 = MapRequirementLeafValue(value2, ref buffer);
            variableValueSets ??= new VariableValues[elements.Length];
            var key = new TwoValueNodeTuple(mappedValue1, mappedValue2);

            if (nextIndex > 0)
            {
                if (!seeded)
                {
                    _seenTwoValueTuples[new TwoValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value)] = 0;
                    seeded = true;
                }

                if (_seenTwoValueTuples.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenTwoValueTuples[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, mappedValue1),
                    new ObjectFieldNode(requirement2.Key, mappedValue2)
                ]));
        }

        _seenTwoValueTuples.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var seeded = false;

        foreach (var result in elements)
        {
            var value1 = ResultDataMapper.Map(result, requirement1.Map, _schema, ref buffer);

            if (value1 is null
                || value1.Kind == SyntaxKind.NullValue
                    && requirement1.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            var value2 = ResultDataMapper.Map(result, requirement2.Map, _schema, ref buffer);

            if (value2 is null
                || value2.Kind == SyntaxKind.NullValue
                    && requirement2.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];
            var key = new TwoValueNodeTuple(value1, value2);

            if (nextIndex > 0)
            {
                if (!seeded)
                {
                    _seenTwoValueTuples[new TwoValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value)] = 0;
                    seeded = true;
                }

                if (_seenTwoValueTuples.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenTwoValueTuples[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, value1),
                    new ObjectFieldNode(requirement2.Key, value2)
                ]));
        }

        _seenTwoValueTuples.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3,
        ref PooledArrayWriter? buffer)
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
                fieldName3,
                ref buffer);
        }

        return BuildVariableValueSetsThreeRequirementsSlowPath(
            elements,
            requirement1,
            requirement2,
            requirement3,
            ref buffer);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsFastPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        OperationRequirement requirement3,
        string fieldName3,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var seeded = false;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName3, out var value3)
                || value3.ValueKind is JsonValueKind.Undefined
                || value3.ValueKind is JsonValueKind.Null
                    && requirement3.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            var mappedValue1 = MapRequirementLeafValue(value1, ref buffer);
            var mappedValue2 = MapRequirementLeafValue(value2, ref buffer);
            var mappedValue3 = MapRequirementLeafValue(value3, ref buffer);
            variableValueSets ??= new VariableValues[elements.Length];
            var key = new ThreeValueNodeTuple(mappedValue1, mappedValue2, mappedValue3);

            if (nextIndex > 0)
            {
                if (!seeded)
                {
                    _seenThreeValueTuples[new ThreeValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value,
                        variableValueSets[0].Values.Fields[2].Value)] = 0;
                    seeded = true;
                }

                if (_seenThreeValueTuples.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenThreeValueTuples[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, mappedValue1),
                    new ObjectFieldNode(requirement2.Key, mappedValue2),
                    new ObjectFieldNode(requirement3.Key, mappedValue3)
                ]));
        }

        _seenThreeValueTuples.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator();
        var nextIndex = 0;
        var seeded = false;

        foreach (var result in elements)
        {
            var value1 = ResultDataMapper.Map(result, requirement1.Map, _schema, ref buffer);

            if (value1 is null
                || (value1.Kind == SyntaxKind.NullValue
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            var value2 = ResultDataMapper.Map(result, requirement2.Map, _schema, ref buffer);

            if (value2 is null
                || (value2.Kind == SyntaxKind.NullValue
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            var value3 = ResultDataMapper.Map(result, requirement3.Map, _schema, ref buffer);

            if (value3 is null
                || (value3.Kind == SyntaxKind.NullValue
                    && requirement3.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Length];
            var key = new ThreeValueNodeTuple(value1, value2, value3);

            if (nextIndex > 0)
            {
                if (!seeded)
                {
                    _seenThreeValueTuples[new ThreeValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value,
                        variableValueSets[0].Values.Fields[2].Value)] = 0;
                    seeded = true;
                }

                if (_seenThreeValueTuples.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths.Add(existingIndex, result.CompactPath);
                    continue;
                }

                _seenThreeValueTuples[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.CompactPath,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, value1),
                    new ObjectFieldNode(requirement2.Key, value2),
                    new ObjectFieldNode(requirement3.Key, value3)
                ]));
        }

        _seenThreeValueTuples.Clear();
        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ObjectValueNode? MapRequirements(
        CompositeResultElement result,
        IReadOnlyList<ObjectFieldNode> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements,
        ref PooledArrayWriter? buffer)
    {
        var fieldCount = forwardedVariables.Count + requirements.Length;

        if (fieldCount == 0)
        {
            return new ObjectValueNode([]);
        }

        var fields = new ObjectFieldNode[fieldCount];
        var index = 0;

        for (var i = 0; i < forwardedVariables.Count; i++)
        {
            fields[index++] = forwardedVariables[i];
        }

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

            fields[index++] = field;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IValueNode MapRequirementLeafValue(
        CompositeResultElement value,
        ref PooledArrayWriter? buffer)
        => value.ValueKind is JsonValueKind.String
            ? ResultDataMapper.GetStringValueNode(value.AssertString())
            : ResultDataMapper.MapLeafValue(value, ref buffer);

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
                var selection = _operation.GetSelectionById(segment);

                if (!element.TryGetProperty(selection.ResponseName, out element))
                {
                    return default;
                }
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

        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetA, clearArray: true);
        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetB, clearArray: true);
        ArrayPool<CompositeResultElement>.Shared.Return(_collectTargetCombined, clearArray: true);

        while (_memory.TryPop(out var memory))
        {
            memory.Dispose();
        }

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

    private sealed class SingleValueNodeComparer : IEqualityComparer<IValueNode>
    {
        public static SingleValueNodeComparer Instance { get; } = new();

        public bool Equals(IValueNode? x, IValueNode? y)
            => SyntaxComparer.BySyntax.Equals(x, y);

        public int GetHashCode(IValueNode obj)
            => SyntaxComparer.BySyntax.GetHashCode(obj);
    }

    private static ImmutableArray<VariableValues> FinalizeVariableValueSets(
        VariableValues[]? variableValueSets,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
    {
        if (variableValueSets is null || nextIndex == 0)
        {
            additionalPaths.Dispose();
            return [];
        }

        additionalPaths.ApplyTo(variableValueSets, nextIndex);
        additionalPaths.Dispose();

        if (variableValueSets.Length != nextIndex)
        {
            Array.Resize(ref variableValueSets, nextIndex);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(variableValueSets);
    }

    private readonly record struct TwoValueNodeTuple(IValueNode Value1, IValueNode Value2);

    private readonly record struct ThreeValueNodeTuple(
        IValueNode Value1,
        IValueNode Value2,
        IValueNode Value3);

    private sealed class TwoValueNodeTupleComparer : IEqualityComparer<TwoValueNodeTuple>
    {
        public static TwoValueNodeTupleComparer Instance { get; } = new();

        public bool Equals(TwoValueNodeTuple x, TwoValueNodeTuple y)
            => SyntaxComparer.BySyntax.Equals(x.Value1, y.Value1)
                && SyntaxComparer.BySyntax.Equals(x.Value2, y.Value2);

        public int GetHashCode(TwoValueNodeTuple obj)
            => HashCode.Combine(
                SyntaxComparer.BySyntax.GetHashCode(obj.Value1),
                SyntaxComparer.BySyntax.GetHashCode(obj.Value2));
    }

    private sealed class ThreeValueNodeTupleComparer : IEqualityComparer<ThreeValueNodeTuple>
    {
        public static ThreeValueNodeTupleComparer Instance { get; } = new();

        public bool Equals(ThreeValueNodeTuple x, ThreeValueNodeTuple y)
            => SyntaxComparer.BySyntax.Equals(x.Value1, y.Value1)
                && SyntaxComparer.BySyntax.Equals(x.Value2, y.Value2)
                && SyntaxComparer.BySyntax.Equals(x.Value3, y.Value3);

        public int GetHashCode(ThreeValueNodeTuple obj)
            => HashCode.Combine(
                SyntaxComparer.BySyntax.GetHashCode(obj.Value1),
                SyntaxComparer.BySyntax.GetHashCode(obj.Value2),
                SyntaxComparer.BySyntax.GetHashCode(obj.Value3));
    }
}
