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

internal sealed class FetchResultStore : IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ISchemaDefinition _schema;
    private readonly IErrorHandler _errorHandler;
    private readonly Operation _operation;
    private readonly ErrorHandlingMode _errorHandlingMode;
    private readonly ulong _includeFlags;
    private readonly ConcurrentStack<IDisposable> _memory = [];
    private readonly List<CompositeResultElement> _collectTargetCurrent = [];
    private readonly List<CompositeResultElement> _collectTargetNext = [];
    private readonly List<CompositeResultElement> _collectTargetCombined = [];
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
        => AddPartialResults(sourcePath, results, responseNames, containsErrors: true);

    public bool AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<string> responseNames,
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
                ? AddSinglePartialResultNoErrors(sourcePath, results[0], responseNames)
                : AddPartialResultsNoErrors(sourcePath, results, responseNames);
        }

        if (results.Length == 1)
        {
            return AddSinglePartialResult(sourcePath, results[0], responseNames);
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
                            responseNames))
                    {
                        return false;
                    }
                }
            }

            return true;
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
        ReadOnlySpan<string> responseNames)
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
                            responseNames))
                    {
                        return false;
                    }
                }
            }

            return true;
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
        ReadOnlySpan<string> responseNames)
    {
        _memory.Push(result);

        var errors = result.Errors;
        var dataElement = GetDataElement(sourcePath, result.Data);
        var errorTrie = GetErrorTrie(sourcePath, errors?.Trie);

        lock (_lock)
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
                responseNames);
        }
    }

    private bool AddSinglePartialResultNoErrors(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ReadOnlySpan<string> responseNames)
    {
        _memory.Push(result);
        var dataElement = GetDataElement(sourcePath, result.Data);

        lock (_lock)
        {
            return SaveSafeResult(
                _result.Data,
                result.Path,
                result.AdditionalPaths.AsSpan(),
                dataElement,
                errorTrie: null,
                responseNames);
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

        lock (_lock)
        {
            var partial = document.Root;
            var data = _result.Data;

            return _valueCompletion.BuildResult(
                partial,
                data, errorTrie: null, responseNames: responseNames);
        }
    }

    public void AddError(IError error)
    {
        _errors ??= [];
        _errors.Add(error);
    }

    public bool AddErrors(IError error, ReadOnlySpan<string> responseNames, params ReadOnlySpan<Path> paths)
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

        return true;
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        Path path,
        ReadOnlySpan<Path> additionalPaths,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ReadOnlySpan<string> responseNames)
    {
        if (!SaveSafeResult(resultData, path, dataElement, errorTrie, responseNames))
        {
            return false;
        }

        for (var i = 0; i < additionalPaths.Length; i++)
        {
            if (!SaveSafeResult(resultData, additionalPaths[i], dataElement, errorTrie, responseNames))
            {
                return false;
            }
        }

        return true;
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        Path path,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ReadOnlySpan<string> responseNames)
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
                responseNames);

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

            if (elements is null)
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
            var combined = _collectTargetCombined;
            combined.Clear();

            foreach (var selectionSet in selectionSets)
            {
                var elements = CollectTargetElements(selectionSet);

                if (elements is not null)
                {
                    combined.AddRange(elements);
                }
            }

            if (combined.Count == 0)
            {
                return [];
            }

            return BuildVariableValueSets(combined, requestVariables, requiredData);
        }
    }

    // Caller must hold _lock for reading.
    private List<CompositeResultElement>? CollectTargetElements(SelectionPath selectionSet)
    {
        var current = _collectTargetCurrent;
        var next = _collectTargetNext;
        current.Clear();
        next.Clear();
        current.Add(_result.Data);

        for (var i = 0; i < selectionSet.Segments.Length; i++)
        {
            var segment = selectionSet.Segments[i];

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                foreach (var element in current)
                {
                    if (element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var value)
                        && value.ValueKind is JsonValueKind.String
                        && value.TextEqualsHelper(segment.Name, isPropertyName: false))
                    {
                        next.Add(element);
                    }
                }
            }
            else if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                foreach (var element in current)
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
                        AppendUnrolledLists(value, next);
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

            var temp = current;
            current = next;
            next = temp;
            next.Clear();

            if (current.Count == 0)
            {
                return null;
            }
        }

        return current;
    }

    private ImmutableArray<VariableValues> BuildVariableValueSets(
        List<CompositeResultElement> elements,
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
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

        foreach (var result in elements)
        {
            var variables = MapRequirements(result, requestVariables, requiredData, ref buffer);

            if (variables is null)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Count];

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<ObjectValueNode, int>(elements.Count, VariableValueComparer.Instance)
                {
                    [variableValueSets[0].Values] = 0
                };

                if (seen.TryGetValue(variables, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[variables] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(result.Path, variables);
        }

        if (buffer is not null)
        {
            _memory.Push(buffer);
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirement(
        List<CompositeResultElement> elements,
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
        List<CompositeResultElement> elements,
        OperationRequirement requirement,
        string fieldName,
        ref PooledArrayWriter? buffer)
    {
        var isNonNull = requirement.Type.Kind == SyntaxKind.NonNullType;
        VariableValues[]? variableValueSets = null;
        Dictionary<IValueNode, int>? seen = null;
        Dictionary<string, int>? seenStrings = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName, out var value)
                || value.ValueKind is JsonValueKind.Undefined)
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.Null && isNonNull)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Count];
            IValueNode mappedValue;

            if (value.ValueKind is JsonValueKind.String)
            {
                var stringValue = value.AssertString();

                if (seenStrings is not null
                    && seenStrings.TryGetValue(stringValue, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                mappedValue = ResultDataMapper.GetStringValueNode(stringValue);
                seenStrings ??= new Dictionary<string, int>(elements.Count, StringComparer.Ordinal);
                seenStrings[stringValue] = nextIndex;
            }
            else
            {
                mappedValue = ResultDataMapper.MapLeafValue(value, ref buffer);

                if (seen is not null
                    && seen.TryGetValue(mappedValue, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen ??= new Dictionary<IValueNode, int>(elements.Count, SingleValueNodeComparer.Instance);
                seen[mappedValue] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([
                    new ObjectFieldNode(
                        requirement.Key,
                        mappedValue)
                ]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementSlowPath(
        List<CompositeResultElement> elements,
        OperationRequirement requirement,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        Dictionary<IValueNode, int>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

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

            variableValueSets ??= new VariableValues[elements.Count];

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<IValueNode, int>(elements.Count, SingleValueNodeComparer.Instance)
                {
                    [variableValueSets[0].Values.Fields[0].Value] = 0
                };

                if (seen.TryGetValue(value, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[value] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([new ObjectFieldNode(requirement.Key, value)]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirements(
        List<CompositeResultElement> elements,
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
        List<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        ref PooledArrayWriter? buffer)
    {
        var requirement1IsNonNull = requirement1.Type.Kind == SyntaxKind.NonNullType;
        var requirement2IsNonNull = requirement2.Type.Kind == SyntaxKind.NonNullType;
        VariableValues[]? variableValueSets = null;
        Dictionary<TwoValueNodeTuple, int>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || value1.ValueKind is JsonValueKind.Null
                    && requirement1IsNonNull)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || value2.ValueKind is JsonValueKind.Null
                    && requirement2IsNonNull)
            {
                continue;
            }

            var mappedValue1 = ResultDataMapper.MapLeafValue(value1, ref buffer);
            var mappedValue2 = ResultDataMapper.MapLeafValue(value2, ref buffer);
            variableValueSets ??= new VariableValues[elements.Count];
            var key = new TwoValueNodeTuple(mappedValue1, mappedValue2);

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<TwoValueNodeTuple, int>(elements.Count, TwoValueNodeTupleComparer.Instance)
                {
                    [new TwoValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value)] = 0
                };

                if (seen.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, mappedValue1),
                    new ObjectFieldNode(requirement2.Key, mappedValue2)
                ]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsSlowPath(
        List<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        Dictionary<TwoValueNodeTuple, int>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

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

            variableValueSets ??= new VariableValues[elements.Count];
            var key = new TwoValueNodeTuple(value1, value2);

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<TwoValueNodeTuple, int>(elements.Count, TwoValueNodeTupleComparer.Instance)
                {
                    [new TwoValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value)] = 0
                };

                if (seen.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, value1),
                    new ObjectFieldNode(requirement2.Key, value2)
                ]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirements(
        List<CompositeResultElement> elements,
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
        List<CompositeResultElement> elements,
        OperationRequirement requirement1,
        string fieldName1,
        OperationRequirement requirement2,
        string fieldName2,
        OperationRequirement requirement3,
        string fieldName3,
        ref PooledArrayWriter? buffer)
    {
        var requirement1IsNonNull = requirement1.Type.Kind == SyntaxKind.NonNullType;
        var requirement2IsNonNull = requirement2.Type.Kind == SyntaxKind.NonNullType;
        var requirement3IsNonNull = requirement3.Type.Kind == SyntaxKind.NonNullType;
        VariableValues[]? variableValueSets = null;
        Dictionary<ThreeValueNodeTuple, int>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(fieldName1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || value1.ValueKind is JsonValueKind.Null
                    && requirement1IsNonNull)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || value2.ValueKind is JsonValueKind.Null
                    && requirement2IsNonNull)
            {
                continue;
            }

            if (!result.TryGetProperty(fieldName3, out var value3)
                || value3.ValueKind is JsonValueKind.Undefined
                || value3.ValueKind is JsonValueKind.Null
                    && requirement3IsNonNull)
            {
                continue;
            }

            var mappedValue1 = ResultDataMapper.MapLeafValue(value1, ref buffer);
            var mappedValue2 = ResultDataMapper.MapLeafValue(value2, ref buffer);
            var mappedValue3 = ResultDataMapper.MapLeafValue(value3, ref buffer);
            variableValueSets ??= new VariableValues[elements.Count];
            var key = new ThreeValueNodeTuple(mappedValue1, mappedValue2, mappedValue3);

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<ThreeValueNodeTuple, int>(elements.Count, ThreeValueNodeTupleComparer.Instance)
                {
                    [new ThreeValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value,
                        variableValueSets[0].Values.Fields[2].Value)] = 0
                };

                if (seen.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, mappedValue1),
                    new ObjectFieldNode(requirement2.Key, mappedValue2),
                    new ObjectFieldNode(requirement3.Key, mappedValue3)
                ]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsSlowPath(
        List<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3,
        ref PooledArrayWriter? buffer)
    {
        VariableValues[]? variableValueSets = null;
        Dictionary<ThreeValueNodeTuple, int>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

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

            var value3 = ResultDataMapper.Map(result, requirement3.Map, _schema, ref buffer);

            if (value3 is null
                || value3.Kind == SyntaxKind.NullValue
                    && requirement3.Type.Kind == SyntaxKind.NonNullType)
            {
                continue;
            }

            variableValueSets ??= new VariableValues[elements.Count];
            var key = new ThreeValueNodeTuple(value1, value2, value3);

            if (nextIndex > 0)
            {
                seen ??= new Dictionary<ThreeValueNodeTuple, int>(elements.Count, ThreeValueNodeTupleComparer.Instance)
                {
                    [new ThreeValueNodeTuple(
                        variableValueSets[0].Values.Fields[0].Value,
                        variableValueSets[0].Values.Fields[1].Value,
                        variableValueSets[0].Values.Fields[2].Value)] = 0
                };

                if (seen.TryGetValue(key, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                seen[key] = nextIndex;
            }

            variableValueSets[nextIndex++] = new VariableValues(
                result.Path,
                new ObjectValueNode([
                    new ObjectFieldNode(requirement1.Key, value1),
                    new ObjectFieldNode(requirement2.Key, value2),
                    new ObjectFieldNode(requirement3.Key, value3)
                ]));
        }

        return FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);
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

    private static void AppendUnrolledLists(
        CompositeResultElement list,
        List<CompositeResultElement> destination)
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
                AppendUnrolledLists(element, destination);
            }
            else
            {
                destination.Add(element);
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

        while (_memory.TryPop(out var memory))
        {
            memory.Dispose();
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
        List<Path>?[]? additionalPaths,
        int nextIndex)
    {
        if (variableValueSets is null || nextIndex == 0)
        {
            return [];
        }

        if (additionalPaths is not null)
        {
            for (var i = 0; i < nextIndex; i++)
            {
                if (additionalPaths[i] is { } paths)
                {
                    variableValueSets[i] = variableValueSets[i] with
                    {
                        AdditionalPaths = [.. paths]
                    };
                }
            }
        }

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
