using System.Buffers;
using System.Buffers.Binary;
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
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Text.Json;
using IntValueNode = HotChocolate.Language.IntValueNode;
using FloatValueNode = HotChocolate.Language.FloatValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using BooleanValueNode = HotChocolate.Language.BooleanValueNode;
using NullValueNode = HotChocolate.Language.NullValueNode;
using EnumValueNode = HotChocolate.Language.EnumValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;
using ObjectValueNode = HotChocolate.Language.ObjectValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;
using System.Text;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore : IDisposable
{
    private const int MaxRetainedDataElementStagingLength = 1024;
    private const int MaxRetainedForwardedVariableCapacity = 16 * 1024;
    private const int MaxStackAllocPathSegments = 32;
    private const int MaxLinearProducerScanLength = 4;

    private static readonly ArrayPool<VariableValues> s_variableValuePool = ArrayPool<VariableValues>.Shared;
    private static readonly ArrayPool<object> s_objectPool = ArrayPool<object>.Shared;
    private static readonly ArrayPool<byte> s_bytePool = ArrayPool<byte>.Shared;
    private static readonly ArrayPool<int> s_intPool = ArrayPool<int>.Shared;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<IDisposable> _memory = [];
    private readonly ArenaBufferWriter _variableWriter = new();
    private bool _variableWriterHasArena;
    private PooledArrayWriter? _forwardedVariableWriter;
    private readonly JsonWriter _jsonWriter;
    private readonly VariableDedupTable _variableDedupTable = new();
    private ArenaBufferWriter? _leafKeyWriter;
    private VariableDedupTable? _leafKeyDedupTable;
    private bool _leafKeyWriterHasArena;
    private FusionSchemaDefinition _schema = default!;
    private IErrorHandler _errorHandler = default!;
    private Operation _operation = default!;
    private ErrorHandlingMode _errorHandlingMode;
    private ulong _includeFlags;
    private ulong _deferFlags;
    private ulong[]? _wideIncludeFlags;
    private ulong[]? _wideDeferFlags;
    private CompositeResultElement[] _collectTargetA = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetB = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectTargetCombined = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private CompositeResultElement[] _collectProducerCombined = ArrayPool<CompositeResultElement>.Shared.Rent(64);
    private SourceResultElement[] _dataElementStaging = [];
    private LeafValue[] _leafValues = [];
    private PathSegmentLocalPool _pathPool = default!;
    private IMemoryArena _arena = default!;
    private HashSet<int[]> _seenPaths = new(ReferenceEqualityComparer.Instance);
    private CompositeResultDocument _result = default!;
    private ValueCompletion _valueCompletion = default!;
    private List<IError>? _errors;
    private Dictionary<Path, IError>? _pocketedErrors;
    private bool _disposed;

    internal FetchResultStore()
    {
        _jsonWriter = new JsonWriter(_variableWriter, new JsonWriterOptions { Indented = false });
    }

    public CompositeResultDocument Result => _result;

    public IReadOnlyList<IError>? Errors => _errors;

    public List<IDisposable> MemoryOwners => _memory;

    internal PathSegmentLocalPool PathPool => _pathPool;

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

        byte[]? rentedUtf8FieldNames = null;
        int[]? rentedUtf8FieldNameEnds = null;
        var asciiByteCount = ComputeAsciiFieldNameBytes(sourcePath);

        var utf8FieldNames = asciiByteCount == 0
            ? default
            : asciiByteCount <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedUtf8FieldNames = s_bytePool.Rent(asciiByteCount));

        var utf8FieldNameEnds = sourcePath.Length == 0
            ? default
            : sourcePath.Length <= MaxStackAllocPathSegments
                ? stackalloc int[MaxStackAllocPathSegments]
                : (rentedUtf8FieldNameEnds = s_intPool.Rent(sourcePath.Length));

        if (sourcePath.Length > 0)
        {
            TranscodeFieldSegments(sourcePath, utf8FieldNames, utf8FieldNameEnds);
        }

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

                dataElementsSpan[i] =
                    GetDataElement(sourcePath, utf8FieldNames, utf8FieldNameEnds, result);
                errorTriesSpan[i] = GetErrorTrie(sourcePath, errors?.Trie);
            }

            lock (_lock)
            {
                var i = 0;

                try
                {
                    if (rootErrors is not null)
                    {
                        _errors ??= [];
                        _errors.AddRange(rootErrors);
                    }

                    var resultData = _result.Data;
                    for (i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        _memory.Add(result);

                        if (!SaveSafeResult(
                                resultData,
                                result.Path,
                                result.AdditionalPaths,
                                dataElementsSpan[i],
                                errorTriesSpan[i],
                                resultSelectionSet))
                        {
                            RegisterRemainingResults(_memory, results, i);
                            return false;
                        }
                    }

                    return true;
                }
                catch
                {
                    RegisterRemainingResults(_memory, results, i);
                    throw;
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

            if (rentedUtf8FieldNames is not null)
            {
                s_bytePool.Return(rentedUtf8FieldNames);
            }

            if (rentedUtf8FieldNameEnds is not null)
            {
                s_intPool.Return(rentedUtf8FieldNameEnds);
            }
        }

        static void RegisterRemainingResults(
            List<IDisposable> _memory,
            ReadOnlySpan<SourceSchemaResult> results,
            int i)
        {
            i++;

            if (i < results.Length)
            {
                for (; i < results.Length; i++)
                {
                    _memory.Add(results[i]);
                }
            }
        }
    }

    private bool AddPartialResultsNoErrors(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ResultSelectionSet resultSelectionSet)
    {
        // Pending merges are consumed serially by OperationPlanExecutor, so this retained buffer
        // cannot be used concurrently by two no-error multi-result merges on the same store.
        var returnDataElements = results.Length > MaxRetainedDataElementStagingLength;
        SourceResultElement[] dataElements;

        if (returnDataElements)
        {
            dataElements = ArrayPool<SourceResultElement>.Shared.Rent(results.Length);
        }
        else
        {
            if (_dataElementStaging.Length < results.Length)
            {
                _dataElementStaging = CreateDataElementStaging(results.Length);
            }

            dataElements = _dataElementStaging;
        }

        var dataElementsSpan = dataElements.AsSpan(0, results.Length);

        byte[]? rentedUtf8FieldNames = null;
        int[]? rentedUtf8FieldNameEnds = null;
        var asciiByteCount = ComputeAsciiFieldNameBytes(sourcePath);

        var utf8FieldNames = asciiByteCount == 0
            ? default
            : asciiByteCount <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedUtf8FieldNames = s_bytePool.Rent(asciiByteCount));

        var utf8FieldNameEnds = sourcePath.Length == 0
            ? default
            : sourcePath.Length <= MaxStackAllocPathSegments
                ? stackalloc int[MaxStackAllocPathSegments]
                : (rentedUtf8FieldNameEnds = s_intPool.Rent(sourcePath.Length));

        if (sourcePath.Length > 0)
        {
            TranscodeFieldSegments(sourcePath, utf8FieldNames, utf8FieldNameEnds);
        }

        try
        {
            for (var i = 0; i < results.Length; i++)
            {
                dataElementsSpan[i] =
                    GetDataElement(sourcePath, utf8FieldNames, utf8FieldNameEnds, results[i]);
            }

            lock (_lock)
            {
                var i = 0;

                try
                {
                    var resultData = _result.Data;

                    for (i = 0; i < results.Length; i++)
                    {
                        var result = results[i];
                        _memory.Add(result);

                        if (!SaveSafeResult(
                                resultData,
                                result.Path,
                                result.AdditionalPaths,
                                dataElementsSpan[i],
                                errorTrie: null,
                                resultSelectionSet))
                        {
                            RegisterRemainingResults(_memory, results, i);
                            return false;
                        }
                    }

                    return true;
                }
                catch
                {
                    RegisterRemainingResults(_memory, results, i);
                    throw;
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

            if (returnDataElements)
            {
                ArrayPool<SourceResultElement>.Shared.Return(dataElements);
            }

            if (rentedUtf8FieldNames is not null)
            {
                s_bytePool.Return(rentedUtf8FieldNames);
            }

            if (rentedUtf8FieldNameEnds is not null)
            {
                s_intPool.Return(rentedUtf8FieldNameEnds);
            }
        }

        static void RegisterRemainingResults(
            List<IDisposable> _memory,
            ReadOnlySpan<SourceSchemaResult> results,
            int i)
        {
            i++;

            if (i < results.Length)
            {
                for (; i < results.Length; i++)
                {
                    _memory.Add(results[i]);
                }
            }
        }
    }

    private static SourceResultElement[] CreateDataElementStaging(int minimumLength)
    {
        Debug.Assert(minimumLength is > 0 and <= MaxRetainedDataElementStagingLength);

        var length = minimumLength switch
        {
            <= 256 => 256,
            <= 512 => 512,
            _ => MaxRetainedDataElementStagingLength
        };

        return new SourceResultElement[length];
    }

    private bool AddSinglePartialResult(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ResultSelectionSet resultSelectionSet)
    {
        var errors = result.Errors;
        var dataElement = GetDataElement(sourcePath, result);
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
                    result.AdditionalPaths,
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
        var dataElement = GetDataElement(sourcePath, result);

        lock (_lock)
        {
            _memory.Add(result);

            try
            {
                return SaveSafeResult(
                    _result.Data,
                    result.Path,
                    result.AdditionalPaths,
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
                path = ref Unsafe.Add(ref path, 1);
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
            _valueCompletion.FinalizeInaccessibleRuntimeTypes(_result.Data);
        }
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        CompactPath path,
        CompactPathSegment additionalPaths,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ResultSelectionSet resultSelectionSet)
    {
        var additionalTargets = new AdditionalTargets(this, additionalPaths);

        if (additionalTargets.Length > 0 && errorTrie is null && !HasPocketedErrors)
        {
            return SaveSafeResultWithLeafCopy(
                resultData,
                path,
                additionalTargets,
                dataElement,
                resultSelectionSet);
        }

        if (!SaveSafeResult(resultData, path, dataElement, errorTrie, resultSelectionSet))
        {
            return false;
        }

        for (var i = 0; i < additionalTargets.Length; i++)
        {
            if (!SaveSafeResult(resultData, additionalTargets, i, dataElement, errorTrie, resultSelectionSet))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Completes the primary target from the source data and assigns its completed leaf values to
    /// the additional targets by response name. Targets that cannot take the leaf values are
    /// completed from the source data.
    /// </summary>
    private bool SaveSafeResultWithLeafCopy(
        CompositeResultElement resultData,
        CompactPath path,
        AdditionalTargets additionalTargets,
        SourceResultElement dataElement,
        ResultSelectionSet resultSelectionSet)
    {
        if (resultData.IsNullOrInvalidated)
        {
            return false;
        }

        var errorCount = _errors?.Count ?? 0;
        var primary = path.IsRoot ? resultData : GetStartResult(path);

        if (!SaveSafeResult(resultData, primary, dataElement, errorTrie: null, resultSelectionSet))
        {
            return false;
        }

        var leafCount = 0;

        try
        {
            var canCopy = errorCount == (_errors?.Count ?? 0)
                && !HasPocketedErrors
                && TryCollectLeafValues(primary, dataElement, resultSelectionSet, ref leafCount);
            var leafValues = _leafValues.AsSpan(0, leafCount);

            for (var i = 0; i < additionalTargets.Length; i++)
            {
                var target = additionalTargets[i];
                Debug.Assert(target.ValueKind is JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined);

                if (target.IsNullOrInvalidated)
                {
                    continue;
                }

                if (canCopy && TryAssignLeafValues(target, leafValues))
                {
                    continue;
                }

                if (!SaveSafeResult(resultData, target, dataElement, errorTrie: null, resultSelectionSet))
                {
                    return false;
                }

                // Once an error is pocketed, the remaining targets are completed from the source data.
                canCopy &= !HasPocketedErrors;
            }

            return true;
        }
        finally
        {
            _leafValues.AsSpan(0, leafCount).Clear();
        }
    }

    /// <summary>
    /// Collects the completed values of <paramref name="primary"/> for every property of
    /// <paramref name="dataElement"/>. Returns <c>false</c> when the source data holds a property
    /// that <paramref name="primary"/> does not select, a property that is not a scalar, or a
    /// completed value that is not a leaf; <paramref name="count"/> then still counts the collected
    /// values.
    /// </summary>
    private bool TryCollectLeafValues(
        CompositeResultElement primary,
        SourceResultElement dataElement,
        ResultSelectionSet resultSelectionSet,
        ref int count)
    {
        if (resultSelectionSet.HasSourceResponseNameMappings
            || dataElement.ValueKind is not JsonValueKind.Object
            || primary.IsNullOrInvalidated
            || !primary.TryGetObjectContext(out var context)
            || context.SelectionSet is not { Type.Kind: TypeKind.Object })
        {
            return false;
        }

        foreach (var property in dataElement.EnumerateObject())
        {
            if (!context.TryGetProperty(property.NameSpan, out var value, out var selection))
            {
                return false;
            }

            if (property.Value.ValueKind is not (JsonValueKind.String
                    or JsonValueKind.Number
                    or JsonValueKind.True
                    or JsonValueKind.False)
                || value.ValueKind is not (JsonValueKind.String
                    or JsonValueKind.Number
                    or JsonValueKind.True
                    or JsonValueKind.False))
            {
                return false;
            }

            if (count == _leafValues.Length)
            {
                Array.Resize(ref _leafValues, Math.Max(8, count * 2));
            }

            _leafValues[count++] = new LeafValue(selection, value);
        }

        return true;
    }

    /// <summary>
    /// Assigns the collected leaf values to the fields of <paramref name="target"/> that have the
    /// same response names. Returns <c>false</c> when a value cannot be assigned; the target may
    /// then hold some of the values and must be completed from the source data.
    /// </summary>
    private static bool TryAssignLeafValues(
        CompositeResultElement target,
        ReadOnlySpan<LeafValue> leafValues)
    {
        if (!target.TryGetObjectContext(out var context)
            || context.SelectionSet is not { Type.Kind: TypeKind.Object })
        {
            return false;
        }

        foreach (var leafValue in leafValues)
        {
            if (!context.TryGetProperty(leafValue.Selection.Utf8ResponseName, out var value, out var selection))
            {
                continue;
            }

            // A completed enum value is only assigned to a field of the same enum type.
            if (selection.IsEnumValue != leafValue.Selection.IsEnumValue
                || (selection.IsEnumValue && !ReferenceEquals(selection.NamedType, leafValue.Selection.NamedType))
                || !value.TrySetLeafValue(leafValue.Value))
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

        var element = path.IsRoot ? resultData : GetStartResult(path);
        return SaveSafeResult(resultData, element, dataElement, errorTrie, resultSelectionSet);
    }

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        CompositeResultElement element,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ResultSelectionSet resultSelectionSet)
    {
        if (resultData.IsNullOrInvalidated)
        {
            return false;
        }

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

    private bool SaveSafeResult(
        CompositeResultElement resultData,
        AdditionalTargets additionalTargets,
        int index,
        SourceResultElement dataElement,
        ErrorTrie? errorTrie,
        ResultSelectionSet resultSelectionSet)
    {
        if (resultData.IsNullOrInvalidated)
        {
            return false;
        }

        return SaveSafeResult(resultData, additionalTargets[index], dataElement, errorTrie, resultSelectionSet);
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
            var combinedCount = CollectCombinedElements(selectionSets, ref _collectTargetCombined);

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

    /// <summary>
    /// Creates the variable value sets of a lookup and fills the targets of entities that the
    /// result already holds at the paths of <paramref name="candidates"/>.
    /// </summary>
    /// <returns>
    /// The variable value sets of the entities that still have to be fetched.
    /// </returns>
    internal ImmutableArray<VariableValues> CreateVariableValueSets(
        ReadOnlySpan<SelectionPath> selectionSets,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData,
        EntityReuseCandidateList candidates,
        out int reusedCount)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requestVariables);
        ArgumentNullException.ThrowIfNull(candidates);

        reusedCount = 0;

        if (requiredData.Length == 0)
        {
            throw new ArgumentException(
                "The required data span must contain at least one requirement.",
                nameof(requiredData));
        }

        lock (_lock)
        {
            var targetCount = CollectCombinedElements(selectionSets, ref _collectTargetCombined);

            if (targetCount == 0)
            {
                return [];
            }

            var variableValueSets = BuildVariableValueSets(
                _collectTargetCombined.AsSpan(0, targetCount),
                requestVariables,
                requiredData);

            // Entities are only reused while the result has no errors.
            if (variableValueSets.IsEmpty
                || _errors is { Count: > 0 }
                || _pocketedErrors is { Count: > 0 })
            {
                return variableValueSets;
            }

            var producerCount = CollectEntityReuseProducersUnsafe(candidates);

            if (producerCount == 0)
            {
                return variableValueSets;
            }

            var producerValueSets = BuildVariableValueSets(
                _collectProducerCombined.AsSpan(0, producerCount),
                requestVariables,
                requiredData);

            return ReuseEntities(variableValueSets, producerValueSets, candidates.ResponseNames, out reusedCount);
        }
    }

    // Caller must hold _lock.
    private int CollectCombinedElements(
        ReadOnlySpan<SelectionPath> selectionSets,
        ref CompositeResultElement[] buffer)
    {
        var combinedCount = 0;

        foreach (var selectionSet in selectionSets)
        {
            var elements = CollectTargetElements(selectionSet);

            if (!elements.IsEmpty)
            {
                EnsureCapacity(
                    ref buffer,
                    combinedCount + elements.Length,
                    combinedCount);
                elements.CopyTo(buffer.AsSpan(combinedCount));
                combinedCount += elements.Length;
            }
        }

        return combinedCount;
    }

    private int CollectEntityReuseProducersUnsafe(EntityReuseCandidateList candidates)
    {
        var candidateCount = CollectCombinedElements(candidates.Paths, ref _collectProducerCombined);
        var producers = _collectProducerCombined.AsSpan(0, candidateCount);
        var producerCount = 0;

        for (var i = 0; i < producers.Length; i++)
        {
            if (HoldsLeafValues(producers[i], candidates.ResponseNames)
                && HasNonNullKeyValue(producers[i], candidates.KeyResponseNames))
            {
                producers[producerCount++] = producers[i];
            }
        }

        return producerCount;
    }

    // Caller must hold _lock.
    private ImmutableArray<VariableValues> ReuseEntities(
        ImmutableArray<VariableValues> variableValueSets,
        ImmutableArray<VariableValues> producerValueSets,
        ReadOnlySpan<string> responseNames,
        out int reusedCount)
    {
        reusedCount = 0;

        if (producerValueSets.IsEmpty)
        {
            return variableValueSets;
        }

        ImmutableArray<VariableValues>.Builder? remaining = null;

        for (var i = 0; i < variableValueSets.Length; i++)
        {
            var variableValues = variableValueSets[i];

            if (TryFindProducer(producerValueSets, variableValues.Values, out var producerPath)
                && TryFillTargets(variableValues, GetStartResult(producerPath), responseNames))
            {
                if (remaining is null)
                {
                    remaining = ImmutableArray.CreateBuilder<VariableValues>(variableValueSets.Length - 1);

                    for (var j = 0; j < i; j++)
                    {
                        remaining.Add(variableValueSets[j]);
                    }
                }

                reusedCount++;
                continue;
            }

            remaining?.Add(variableValueSets[i]);
        }

        return remaining is null ? variableValueSets : remaining.ToImmutable();
    }

    // Caller must hold _lock and pass the value sets of the last build, which the dedup table indexes.
    private bool TryFindProducer(
        ImmutableArray<VariableValues> producerValueSets,
        JsonSegment values,
        out CompactPath producerPath)
    {
        if (producerValueSets.Length <= MaxLinearProducerScanLength)
        {
            foreach (var producer in producerValueSets)
            {
                if (producer.Values.Equals(values))
                {
                    producerPath = producer.Path;
                    return true;
                }
            }
        }
        else if (_variableDedupTable.TryGet(producerValueSets, values, out var index))
        {
            producerPath = producerValueSets[index].Path;
            return true;
        }

        producerPath = default;
        return false;
    }

    // Caller must hold _lock.
    private bool TryFillTargets(
        VariableValues variableValues,
        CompositeResultElement producer,
        ReadOnlySpan<string> responseNames)
    {
        if (!TryFillTarget(GetStartResult(variableValues.Path), producer, responseNames))
        {
            return false;
        }

        var additionalTargets = new AdditionalTargets(this, variableValues.AdditionalPaths);

        for (var i = 0; i < additionalTargets.Length; i++)
        {
            if (!TryFillTarget(additionalTargets[i], producer, responseNames))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryFillTarget(
        CompositeResultElement target,
        CompositeResultElement producer,
        ReadOnlySpan<string> responseNames)
    {
        if (target.IsNullOrInvalidated)
        {
            return true;
        }

        foreach (var responseName in responseNames)
        {
            if (!target.TryGetProperty(responseName, out var targetValue)
                || !producer.TryGetProperty(responseName, out var producerValue)
                || !targetValue.TrySetLeafValue(producerValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HoldsLeafValues(CompositeResultElement element, ReadOnlySpan<string> responseNames)
    {
        if (element.IsNullOrInvalidated)
        {
            return false;
        }

        foreach (var responseName in responseNames)
        {
            if (!element.TryGetProperty(responseName, out var value)
                || value.IsInvalidated
                || value.ValueKind is not (JsonValueKind.String
                    or JsonValueKind.Number
                    or JsonValueKind.True
                    or JsonValueKind.False))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasNonNullKeyValue(CompositeResultElement element, ReadOnlySpan<string> keyResponseNames)
    {
        foreach (var responseName in keyResponseNames)
        {
            if (element.TryGetProperty(responseName, out var value)
                && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                return true;
            }
        }

        return false;
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
                    "A deferred incremental plan fetch references a requirement that was not imported.");
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
                IOutputTypeDefinition? segmentType = null;

                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];

                    if (!element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var value)
                        || value.ValueKind is not JsonValueKind.String)
                    {
                        continue;
                    }

                    // Fast path: the runtime __typename equals the type condition exactly.
                    if (value.TextEqualsHelper(segment.Name, isPropertyName: false))
                    {
                        AddToBuffer(ref next, ref nextCount, element);
                        continue;
                    }

                    // The type condition names an abstract type; accept elements whose
                    // runtime type is a subtype of it.
                    segmentType ??= _schema.Types.GetType<IOutputTypeDefinition>(segment.Name);

                    if (segmentType.IsAssignableFrom(element.AssertSelectionSet().Type))
                    {
                        AddToBuffer(ref next, ref nextCount, element);
                    }
                }
            }
            else if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                var lookupMemo = default(PropertyLookupMemo);

                for (var j = 0; j < currentCount; j++)
                {
                    var element = current[j];
                    if (!element.TryGetProperty(segment.Name, ref lookupMemo, out var value))
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

                    throw ThrowHelper.InvalidTargetValueKind(
                        selectionSet.Slice(i + 1),
                        value.Path,
                        valueKind);
                }
            }

            (current, next) = (next, current);
            (currentCount, nextCount) = (nextCount, 0);

            if (currentCount == 0)
            {
                // Update collection state.
                _collectTargetA = current;
                _collectTargetB = next;
                return [];
            }
        }

        // Update collection state.
        _collectTargetA = current;
        _collectTargetB = next;
        return current.AsSpan(0, currentCount);
    }

    public ImmutableArray<CompactPath> GetResultPaths(SelectionPath selectionSet)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(selectionSet);

        lock (_lock)
        {
            if (selectionSet.IsRoot)
            {
                return [CompactPath.Root];
            }

            var elements = CollectTargetElements(selectionSet);

            if (elements.IsEmpty)
            {
                return [];
            }

            var paths = ImmutableArray.CreateBuilder<CompactPath>(elements.Length);

            foreach (var element in elements)
            {
                paths.Add(element.CompactPath);
            }

            return paths.MoveToImmutable();
        }
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsFromSnapshot(
        ImmutableArray<VariableValues> importedEntries,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        _variableDedupTable.Initialize(importedEntries.Length);

        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(importedEntries.Length);
        var nextIndex = 0;
        var forwardedVariables = SerializeForwardedVariables(requestVariables, importedEntries.Length);

        foreach (var importedEntry in importedEntries)
        {
            if (importedEntry.IsEmpty)
            {
                continue;
            }

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            WriteForwardedVariables(requestVariables, forwardedVariables);

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
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var forwardedVariables = SerializeForwardedVariables(requestVariables, elements.Length);

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            WriteForwardedVariables(requestVariables, forwardedVariables);

            // Write requirement fields.
            var failed = false;

            for (var i = 0; i < requiredData.Length; i++)
            {
                var requirement = requiredData[i];
                _jsonWriter.WritePropertyName(requirement.Key);

                if (!ResultDataMapper.TryMap(
                    result,
                    requirement.Map,
                    requirement.Type,
                    _schema,
                    requirement.InternalAlias,
                    _jsonWriter))
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
                result, startPosition, ref additionalPaths, nextIndex);

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

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirement(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        // The fast path copies values verbatim and only guards top-level nulls,
        // so list-typed requirements take the slow path where the mapper checks
        // element nullability.
        if (TryGetSimpleRequirementFieldName(requirement, out var fieldName)
            && !requirement.Type.IsListType())
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
        ResetLeafKeys(elements.Length);
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var isNonNullRequirement = requirement.Type.Kind is SyntaxKind.NonNullType;
        var lookupName = requirement.InternalAlias ?? fieldName;
        var lookupMemo = default(PropertyLookupMemo);

        for (var i = 0; i < elements.Length; i++)
        {
            var result = elements[i];

            if (!result.TryGetProperty(lookupName, ref lookupMemo, out var value))
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

            var leafKey = StartLeafKey();

            if (TryAppendLeafKey(ref leafKey, value)
                && TryGetLeafKeyDuplicate(ref leafKey, out var duplicateIndex))
            {
                additionalPaths.Add(duplicateIndex, result);
                continue;
            }

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;

            // Write variable JSON: {"key":rawValue}
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);
            WriteCompositeResultValue(value);
            _jsonWriter.WriteEndObject();

            // we try to create a VariableValues object,
            // if that fails the variables already were created and we move on.
            var entry = TryCreateVariableValues(
                result,
                startPosition,
                ref additionalPaths,
                nextIndex,
                out var dedupIndex);
            AddLeafKey(ref leafKey, dedupIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsSingleRequirementSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var requirementType = requirement.Type;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement.Map,
                requirementType,
                _schema,
                requirement.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        // The fast path copies values verbatim and only guards top-level nulls,
        // so list-typed requirements take the slow path where the mapper checks
        // element nullability.
        if (TryGetSimpleRequirementFieldName(requirement1, out var fieldName1)
            && !requirement1.Type.IsListType()
            && TryGetSimpleRequirementFieldName(requirement2, out var fieldName2)
            && !requirement2.Type.IsListType())
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
        ResetLeafKeys(elements.Length);
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var lookupName1 = requirement1.InternalAlias ?? fieldName1;
        var lookupName2 = requirement2.InternalAlias ?? fieldName2;
        var lookupMemo1 = default(PropertyLookupMemo);
        var lookupMemo2 = default(PropertyLookupMemo);

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(lookupName1, ref lookupMemo1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(lookupName2, ref lookupMemo2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            var leafKey = StartLeafKey();

            if (TryAppendLeafKey(ref leafKey, value1)
                && TryAppendLeafKey(ref leafKey, value2)
                && TryGetLeafKeyDuplicate(ref leafKey, out var duplicateIndex))
            {
                additionalPaths.Add(duplicateIndex, result);
                continue;
            }

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result, startPosition, ref additionalPaths, nextIndex, out var dedupIndex);
            AddLeafKey(ref leafKey, dedupIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsTwoRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var requirementType1 = requirement1.Type;
        var requirementType2 = requirement2.Type;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement1.Map,
                requirementType1,
                _schema,
                requirement1.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement2.Map,
                requirementType2,
                _schema,
                requirement2.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirements(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        // The fast path copies values verbatim and only guards top-level nulls,
        // so list-typed requirements take the slow path where the mapper checks
        // element nullability.
        if (TryGetSimpleRequirementFieldName(requirement1, out var fieldName1)
            && !requirement1.Type.IsListType()
            && TryGetSimpleRequirementFieldName(requirement2, out var fieldName2)
            && !requirement2.Type.IsListType()
            && TryGetSimpleRequirementFieldName(requirement3, out var fieldName3)
            && !requirement3.Type.IsListType())
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
        ResetLeafKeys(elements.Length);
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var lookupName1 = requirement1.InternalAlias ?? fieldName1;
        var lookupName2 = requirement2.InternalAlias ?? fieldName2;
        var lookupName3 = requirement3.InternalAlias ?? fieldName3;
        var lookupMemo1 = default(PropertyLookupMemo);
        var lookupMemo2 = default(PropertyLookupMemo);
        var lookupMemo3 = default(PropertyLookupMemo);

        foreach (var result in elements)
        {
            if (!result.TryGetProperty(lookupName1, ref lookupMemo1, out var value1)
                || value1.ValueKind is JsonValueKind.Undefined
                || (value1.ValueKind is JsonValueKind.Null
                    && requirement1.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(lookupName2, ref lookupMemo2, out var value2)
                || value2.ValueKind is JsonValueKind.Undefined
                || (value2.ValueKind is JsonValueKind.Null
                    && requirement2.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            if (!result.TryGetProperty(lookupName3, ref lookupMemo3, out var value3)
                || value3.ValueKind is JsonValueKind.Undefined
                || (value3.ValueKind is JsonValueKind.Null
                    && requirement3.Type.Kind == SyntaxKind.NonNullType))
            {
                continue;
            }

            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            var leafKey = StartLeafKey();

            if (TryAppendLeafKey(ref leafKey, value1)
                && TryAppendLeafKey(ref leafKey, value2)
                && TryAppendLeafKey(ref leafKey, value3)
                && TryGetLeafKeyDuplicate(ref leafKey, out var duplicateIndex))
            {
                additionalPaths.Add(duplicateIndex, result);
                continue;
            }

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName(requirement1.Key);
            WriteCompositeResultValue(value1);
            _jsonWriter.WritePropertyName(requirement2.Key);
            WriteCompositeResultValue(value2);
            _jsonWriter.WritePropertyName(requirement3.Key);
            WriteCompositeResultValue(value3);
            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(
                result, startPosition, ref additionalPaths, nextIndex, out var dedupIndex);
            AddLeafKey(ref leafKey, dedupIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    private ImmutableArray<VariableValues> BuildVariableValueSetsThreeRequirementsSlowPath(
        ReadOnlySpan<CompositeResultElement> elements,
        OperationRequirement requirement1,
        OperationRequirement requirement2,
        OperationRequirement requirement3)
    {
        VariableValues[]? variableValueSets = null;
        var additionalPaths = new AdditionalPathAccumulator(elements.Length);
        var nextIndex = 0;
        var requirementType1 = requirement1.Type;
        var requirementType2 = requirement2.Type;
        var requirementType3 = requirement3.Type;

        foreach (var result in elements)
        {
            variableValueSets ??= s_variableValuePool.Rent(elements.Length);

            _jsonWriter.Reset(GetVariableWriter());
            var startPosition = _variableWriter.Position;
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName(requirement1.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement1.Map,
                requirementType1,
                _schema,
                requirement1.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement2.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement2.Map,
                requirementType2,
                _schema,
                requirement2.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WritePropertyName(requirement3.Key);

            if (!ResultDataMapper.TryMap(
                result,
                requirement3.Map,
                requirementType3,
                _schema,
                requirement3.InternalAlias,
                _jsonWriter))
            {
                _variableWriter.ResetTo(startPosition);
                continue;
            }

            _jsonWriter.WriteEndObject();

            var entry = TryCreateVariableValues(result, startPosition, ref additionalPaths, nextIndex);

            if (entry is null)
            {
                continue;
            }

            variableValueSets[nextIndex++] = entry.Value;
        }

        return FinalizeVariableValueSets(variableValueSets, ref additionalPaths, nextIndex);
    }

    /// <summary>
    /// Gets the variable writer, which writes to the memory arena of the current request or event.
    /// </summary>
    private ArenaBufferWriter GetVariableWriter()
    {
        if (!_variableWriterHasArena)
        {
            _variableWriter.Reset(_arena);
            _variableWriterHasArena = true;
        }

        return _variableWriter;
    }

    /// <summary>
    /// Clears the leaf keys of the previous lookup and sizes the key table for
    /// <paramref name="capacity"/> targets. The keys are written to the memory arena of the request.
    /// </summary>
    private void ResetLeafKeys(int capacity)
    {
        if (_leafKeyWriter is null || _leafKeyDedupTable is null)
        {
            _leafKeyWriter = new ArenaBufferWriter(_arena);
            _leafKeyDedupTable = new VariableDedupTable();
        }
        else if (_leafKeyWriterHasArena)
        {
            _leafKeyWriter.Reset();
        }
        else
        {
            _leafKeyWriter.Reset(_arena);
        }

        _leafKeyWriterHasArena = true;
        _leafKeyDedupTable.Initialize(capacity);
    }

    private LeafKey StartLeafKey()
    {
        Debug.Assert(_leafKeyWriter is not null && _leafKeyDedupTable is not null);
        return new LeafKey(_leafKeyWriter, _leafKeyDedupTable, _leafKeyWriter.Position);
    }

    /// <summary>
    /// Appends the kind marker, length and raw bytes of a leaf value to the key.
    /// Discards the key and returns <c>false</c> when the value is not a non-null leaf.
    /// </summary>
    private static bool TryAppendLeafKey(ref LeafKey key, CompositeResultElement value)
    {
        if (!value.TryGetLeafValueKey(out var kind, out var raw))
        {
            DiscardLeafKey(ref key);
            return false;
        }

        var length = raw.Length + 5;
        var span = key.Writer.GetSpan(length);
        span[0] = kind;
        BinaryPrimitives.WriteInt32LittleEndian(span[1..], raw.Length);
        raw.CopyTo(span[5..]);
        key.Writer.Advance(length);
        key.Hasher.Add(kind);
        key.Hasher.AddBytes(raw);
        return true;
    }

    /// <summary>
    /// Returns the variable set index of an earlier target with the same key and discards the key,
    /// or keeps the key so it can be registered with <see cref="AddLeafKey"/>.
    /// </summary>
    private static bool TryGetLeafKeyDuplicate(ref LeafKey key, out int index)
    {
        var length = key.Writer.Position - key.Start;
        key.Hash = key.Hasher.ToHashCode();
        key.Length = length;

        if (key.Table.TryGet(key.Writer, key.Hash, key.Start, length, out index))
        {
            DiscardLeafKey(ref key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the bytes of the key from the writer and marks the key as invalid.
    /// </summary>
    private static void DiscardLeafKey(ref LeafKey key)
    {
        key.Writer.ResetTo(key.Start);
        key.IsValid = false;
    }

    private static void AddLeafKey(ref LeafKey key, int index)
    {
        if (key.IsValid)
        {
            key.Table.Add(key.Hash, index, key.Start, key.Length);
        }
    }

    internal static bool TryGetSimpleRequirementFieldName(
        OperationRequirement requirement,
        [NotNullWhen(true)] out string? fieldName)
    {
        if (requirement.Map is PathNode
            {
                TypeName: null,
                PathSegment:
                {
                    TypeName: null,
                    PathSegment: null
                } pathSegment
            })
        {
            fieldName = requirement.InternalAlias ?? pathSegment.FieldName.Value;
            return true;
        }

        fieldName = null;
        return false;
    }

    /// <summary>
    /// Creates the paths of the elements at the given cursors of <paramref name="document"/>,
    /// which must be the result document of the current request.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="document"/> is no longer the result document of this store.
    /// </exception>
    internal CompactPath[] CreateCompactPaths(CompositeResultDocument document, ReadOnlySpan<int> cursors)
    {
        lock (_lock)
        {
            if (!ReferenceEquals(_result, document))
            {
                throw ThrowHelper.LookupTargetPathsReleased();
            }

            var paths = new CompactPath[cursors.Length];

            for (var i = 0; i < cursors.Length; i++)
            {
                paths[i] = document.CreateCompactPath(new CompositeResultDocument.Cursor(cursors[i]), pooled: false);
            }

            return paths;
        }
    }

    private VariableValues? TryCreateVariableValues(
        CompositeResultElement target,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex)
        => TryCreateVariableValues(target, startPosition, ref additionalPaths, nextIndex, out _);

    private VariableValues? TryCreateVariableValues(
        CompositeResultElement target,
        int startPosition,
        ref AdditionalPathAccumulator additionalPaths,
        int nextIndex,
        out int dedupIndex)
    {
        var length = _variableWriter.Position - startPosition;
        var hash = _variableWriter.GetHashCode(startPosition, length);

        // A target whose variables were already written for an earlier target is merged from
        // the same fetch, so it is tracked as an additional target of the existing entry.
        if (_variableDedupTable.TryGet(_variableWriter, hash, startPosition, length, out var existingIndex))
        {
            dedupIndex = existingIndex;
            additionalPaths.Add(existingIndex, target);
            _variableWriter.ResetTo(startPosition);
            return null;
        }

        dedupIndex = nextIndex;
        _variableDedupTable.Add(hash, nextIndex, startPosition, length);
        return new VariableValues(
            target.CompactPath,
            JsonSegment.Create(_variableWriter.GetSequence(startPosition, length)));
    }

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
        if (_variableDedupTable.TryGet(_variableWriter, hash, startPosition, length, out var existingIndex))
        {
            dedupIndex = existingIndex;
            additionalPaths.Add(existingIndex, path);
            _variableWriter.ResetTo(startPosition);
            return null;
        }

        dedupIndex = nextIndex;
        _variableDedupTable.Add(hash, nextIndex, startPosition, length);
        return new VariableValues(path, JsonSegment.Create(_variableWriter.GetSequence(startPosition, length)));
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

    private ReadOnlySpan<byte> SerializeForwardedVariables(
        IReadOnlyList<ObjectFieldNode> requestVariables,
        int entityCount)
    {
        if (entityCount < 2 || requestVariables.Count == 0)
        {
            return [];
        }

        var writer = _forwardedVariableWriter ??= new PooledArrayWriter();
        writer.Reset();
        _jsonWriter.Reset(writer);
        _jsonWriter.WriteStartObject();

        for (var i = 0; i < requestVariables.Count; i++)
        {
            var field = requestVariables[i];
            _jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(field.Value);
        }

        _jsonWriter.WriteEndObject();
        _jsonWriter.Reset(GetVariableWriter());

        return writer.WrittenSpan[1..^1];
    }

    private void WriteForwardedVariables(
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<byte> serialized)
    {
        if (!serialized.IsEmpty)
        {
            _jsonWriter.WriteRawProperties(serialized);
            return;
        }

        for (var i = 0; i < requestVariables.Count; i++)
        {
            var field = requestVariables[i];
            _jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(field.Value);
        }
    }

    private void WriteValueNode(IValueNode value)
    {
        switch (value)
        {
            case NullValueNode:
                _jsonWriter.WriteNullValue();
                break;

            case StringValueNode sv:
                _jsonWriter.WriteStringValue(sv.AsSpan());
                break;

            case IntValueNode iv:
                _jsonWriter.WriteRawValue(iv.AsSpan());
                break;

            case FloatValueNode fv:
                _jsonWriter.WriteRawValue(fv.AsSpan());
                break;

            case BooleanValueNode bv:
                _jsonWriter.WriteBooleanValue(bv.Value);
                break;

            case EnumValueNode ev:
                _jsonWriter.WriteStringValue(ev.AsSpan());
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

    private void WriteCompositeResultValue(CompositeResultElement value)
        => value.WriteTo(_jsonWriter);

    internal VariableValues CreateVariableValueSets(
        CompactPath path,
        IVariableValueCollection variables,
        ReadOnlySpan<string> forwardedVariables)
    {
        _jsonWriter.Reset(GetVariableWriter());
        var startPosition = _variableWriter.Position;
        _jsonWriter.WriteStartObject();

        if (!variables.IsEmpty)
        {
            foreach (var name in forwardedVariables)
            {
                if (variables.TryGetValue<IValueNode>(name, out var value))
                {
                    _jsonWriter.WritePropertyName(name);
                    WriteValueNode(value);
                }
            }
        }

        _jsonWriter.WriteEndObject();
        var length = _variableWriter.Position - startPosition;
        return new VariableValues(path, JsonSegment.Create(_variableWriter.GetSequence(startPosition, length)));
    }

    internal VariableValues CreateVariableValueSets(
        CompactPath path,
        IReadOnlyList<ObjectFieldNode> fields)
    {
        _jsonWriter.Reset(GetVariableWriter());
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
        return new VariableValues(path, JsonSegment.Create(_variableWriter.GetSequence(startPosition, length)));
    }

    /// <summary>
    /// Imports variable value sets into this store for a child incremental plan.
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

        var writer = GetVariableWriter();
        var startPosition = writer.Position;
        foreach (var memory in source.AsSequence())
        {
            var span = writer.GetSpan(memory.Length);
            memory.Span.CopyTo(span);
            writer.Advance(memory.Length);
        }

        var length = writer.Position - startPosition;
        return JsonSegment.Create(writer.GetSequence(startPosition, length));
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

    private SourceResultElement GetDataElement(SelectionPath sourcePath, SourceSchemaResult result)
    {
        // A source schema client can resolve the root field of a response while it reads it. The
        // element it hands over with the result stands in for the first segment of the source
        // path, so the walk continues after that segment.
        var lookupData = result.LookupData;
        var current = lookupData ?? result.Data;

        for (var i = lookupData is null ? 0 : 1; i < sourcePath.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                // An intermediate value on the path is not an object. If it is null, the target
                // value null-propagates from here, so we surface the null element (not Undefined)
                // and let value completion integrate it together with any source error.
                return current.ValueKind is JsonValueKind.Null ? current : default;
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
                    if (!TryMatchInlineFragmentType(current, segment.Name))
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

    /// <summary>
    /// Same as <see cref="GetDataElement(SelectionPath, SourceSchemaResult)"/>, but uses
    /// pre-encoded segment names so a batch merge encodes the path once and every result
    /// reuses it.
    /// </summary>
    private SourceResultElement GetDataElement(
        SelectionPath sourcePath,
        ReadOnlySpan<byte> utf8FieldNames,
        ReadOnlySpan<int> utf8FieldNameEnds,
        SourceSchemaResult result)
    {
        var lookupData = result.LookupData;
        var current = lookupData ?? result.Data;
        var firstSegment = lookupData is null ? 0 : 1;
        var start = firstSegment > 0 && sourcePath.Length > 0
            ? utf8FieldNameEnds[firstSegment - 1]
            : 0;

        for (var i = firstSegment; i < sourcePath.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return current.ValueKind is JsonValueKind.Null ? current : default;
            }

            var segment = sourcePath[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field:
                    var end = utf8FieldNameEnds[i];

                    if (!current.TryGetProperty(utf8FieldNames[start..end], out current))
                    {
                        return default;
                    }

                    start = end;
                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    if (!TryMatchInlineFragmentType(current, segment.Name))
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

    private bool TryMatchInlineFragmentType(SourceResultElement current, string typeCondition)
    {
        if (!current.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var typeNameProperty)
            || typeNameProperty.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        // Fast path: exact __typename match. On a miss the type condition may name an
        // abstract type, so accept the element when its runtime type is a subtype.
        return typeNameProperty.TextEqualsHelper(typeCondition, isPropertyName: false)
            || IsRuntimeTypeAssignableTo(typeCondition, typeNameProperty.GetString());
    }

    /// <summary>
    /// Returns the total byte length of all root and field segment names in the path.
    /// GraphQL names are ASCII, so one character is one byte. Inline fragment segments
    /// are skipped.
    /// </summary>
    private static int ComputeAsciiFieldNameBytes(SelectionPath sourcePath)
    {
        var byteCount = 0;

        for (var i = 0; i < sourcePath.Length; i++)
        {
            var segment = sourcePath[i];

            if (segment.Kind is SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field)
            {
                byteCount += segment.Name.Length;
            }
        }

        return byteCount;
    }

    /// <summary>
    /// Encodes all root and field segment names into <paramref name="utf8FieldNames"/>.
    /// <paramref name="utf8FieldNameEnds"/> holds each segment's exclusive end offset.
    /// Inline fragment segments write no bytes and repeat the previous offset.
    /// </summary>
    private static void TranscodeFieldSegments(
        SelectionPath sourcePath,
        Span<byte> utf8FieldNames,
        Span<int> utf8FieldNameEnds)
    {
        var offset = 0;

        for (var i = 0; i < sourcePath.Length; i++)
        {
            var segment = sourcePath[i];

            if (segment.Kind is SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field)
            {
                var status = Ascii.FromUtf16(segment.Name, utf8FieldNames[offset..], out var written);
                Debug.Assert(status is OperationStatus.Done, "GraphQL names are ASCII.");
                offset += written;
            }

            utf8FieldNameEnds[i] = offset;
        }
    }

    private bool IsRuntimeTypeAssignableTo(string typeCondition, string? runtimeTypeName)
    {
        if (runtimeTypeName is null
            || !_schema.Types.TryGetType<IOutputTypeDefinition>(typeCondition, out var conditionType)
            || !_schema.Types.TryGetType<IOutputTypeDefinition>(runtimeTypeName, out var runtimeType))
        {
            return false;
        }

        return conditionType.IsAssignableFrom(runtimeType);
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

            // Source schema error paths only carry response names and list indices, never type
            // conditions, so inline-fragment segments have no corresponding level in the trie.
            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                continue;
            }

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
            // Track list containers that need imported elements.
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

        // An interface-typed named type is an @interfaceObject stand-in position that has not yet
        // recovered its concrete identity; it initializes interface-typed against the interface's
        // declared fields. Unions carry no such single type context and remain unsupported here.
        if (selection.Type.NamedType() is not IComplexTypeDefinition complexType)
        {
            throw new InvalidOperationException(
                "Cannot initialize an intermediate target object for an abstract selection.");
        }

        var selectionSet = selection.DeclaringSelectionSet.DeclaringOperation
            .GetSelectionSet(selection, complexType);

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
        ArrayPool<CompositeResultElement>.Shared.Return(_collectProducerCombined, clearArray: true);

        foreach (var memory in _memory)
        {
            memory.Dispose();
        }

        _memory.Clear();

        _variableDedupTable.Dispose();
        _variableWriter.Dispose();
        _leafKeyDedupTable?.Dispose();
        _leafKeyWriter?.Dispose();
        _forwardedVariableWriter?.Dispose();
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

        // Paths created on first access are not rented from the path pool, so they are not returned.
        if (result.AdditionalPaths.Source is not null)
        {
            return;
        }

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

    private ImmutableArray<VariableValues> FinalizeVariableValueSets(
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

        additionalPaths.ApplyTo(variableValueSets, nextIndex, this, _result);
        additionalPaths.Dispose();

        var span = variableValueSets.AsSpan(0, nextIndex);
        var result = span.ToArray();
        span.Clear();
        s_variableValuePool.Return(variableValueSets);

        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    private readonly record struct LeafValue(Selection Selection, CompositeResultElement Value);

    /// <summary>
    /// Resolves the additional targets of a source schema result to the elements currently at their
    /// paths.
    /// </summary>
    private readonly ref struct AdditionalTargets
    {
        private readonly FetchResultStore _store;
        private readonly CompactPathSegment _segment;
        private readonly DuplicateTargetCompactPathSource? _cursors;

        public AdditionalTargets(FetchResultStore store, CompactPathSegment segment)
        {
            _store = store;
            _segment = segment;
            _cursors = segment.Source is DuplicateTargetCompactPathSource cursors
                && ReferenceEquals(cursors.Document, store._result)
                    ? cursors
                    : null;
        }

        public int Length => _segment.Length;

        public CompositeResultElement this[int index]
            => _cursors is { IsCurrent: true } cursors
                ? cursors.GetElement(_segment.Offset + index)
                : _store.GetStartResult(_segment[index]);
    }

    /// <summary>
    /// A key built from the raw leaf values of a lookup target. Targets with equal keys have
    /// equal variable values. The default key is invalid.
    /// </summary>
    private struct LeafKey(ArenaBufferWriter writer, VariableDedupTable table, int start)
    {
        public readonly ArenaBufferWriter Writer = writer;
        public readonly VariableDedupTable Table = table;
        public readonly int Start = start;
        public HashCode Hasher;
        public int Length;
        public int Hash;
        public bool IsValid = true;
    }

    private sealed class VariableDedupTable : IDisposable
    {
        private const int DefaultBucketSize = 4;
        private const int DefaultBucketCount = 16;
        private const int TrackedSlotCapacity = 16;

        private Entry[] _table = RentClearedTable(DefaultBucketCount * DefaultBucketSize);
        private int[] _writtenSlots = [];
        private int _bucketCount = DefaultBucketCount;
        private readonly int _bucketSize = DefaultBucketSize;
        private int _count;
        private int _writtenCount;
        private bool _clearFullTable;

        public void Initialize(int capacity)
        {
            var bucketCount = NextPowerOfTwo(Math.Max(capacity, DefaultBucketCount));
            var totalSize = bucketCount * _bucketSize;

            ClearPreviousEntries();

            if (_table.Length < totalSize)
            {
                Resize(totalSize);
            }

            _bucketCount = bucketCount;
        }

        /// <summary>
        /// Gets the index of an entry whose bytes in <paramref name="writer"/> equal the
        /// <paramref name="length"/> bytes at <paramref name="location"/>.
        /// </summary>
        public bool TryGet(
            ArenaBufferWriter writer,
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
                    && writer.SequenceEqual(entry.Location, location, length))
                {
                    existingIndex = entry.Index - 1;
                    return true;
                }
            }

            existingIndex = -1;
            return false;
        }

        /// <summary>
        /// Gets the index of an entry whose bytes in <paramref name="writer"/> equal the
        /// <paramref name="length"/> bytes at <paramref name="location"/>.
        /// </summary>
        public bool TryGet(
            ChunkedArrayWriter writer,
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
                    && writer.SequenceEqual(entry.Location, location, length))
                {
                    existingIndex = entry.Index - 1;
                    return true;
                }
            }

            existingIndex = -1;
            return false;
        }

        /// <summary>
        /// Gets the index of an entry whose variable value set in <paramref name="valueSets"/>
        /// has <paramref name="values"/>.
        /// </summary>
        public bool TryGet(
            ImmutableArray<VariableValues> valueSets,
            JsonSegment values,
            out int existingIndex)
        {
            var hash = values.GetHashCode();
            var length = values.Length;
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
                    && valueSets[entry.Index - 1].Values.Equals(values))
                {
                    existingIndex = entry.Index - 1;
                    return true;
                }
            }

            existingIndex = -1;
            return false;
        }

        /// <summary>
        /// Adds an entry. The entry is not added when its bucket is full and the table already
        /// has at least two buckets per entry, so a later lookup of the same key misses.
        /// </summary>
        public void Add(int hash, int index, int location, int length)
        {
            while (!TryInsert(hash, index, location, length))
            {
                if (_bucketCount >= 2 * _count)
                {
                    return;
                }

                Grow();
            }

            _count++;
        }

        /// <summary>
        /// Replaces the table with one of the default size when its slot capacity exceeds
        /// <paramref name="maxEntryCount"/>.
        /// </summary>
        public void Trim(int maxEntryCount)
        {
            if (_table.Length <= maxEntryCount)
            {
                return;
            }

            ArrayPool<Entry>.Shared.Return(_table);
            _table = RentClearedTable(DefaultBucketCount * _bucketSize);
            _bucketCount = DefaultBucketCount;
            _count = 0;
            ResetClearState();
        }

        public void Dispose()
        {
            if (_table.Length > 0)
            {
                ArrayPool<Entry>.Shared.Return(_table);
                _table = [];
            }

            if (_writtenSlots.Length > 0)
            {
                ArrayPool<int>.Shared.Return(_writtenSlots);
                _writtenSlots = [];
            }

            _count = 0;
            ResetClearState();
        }

        private bool TryInsert(int hash, int index, int location, int length)
        {
            var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
            var start = bucket * _bucketSize;
            var end = start + _bucketSize;

            for (var s = start; s < end; s++)
            {
                ref var entry = ref _table[s];

                if (entry.Index == 0)
                {
                    RecordWrittenSlot(s);

                    entry.Hash = hash;
                    entry.Index = index + 1;
                    entry.Location = location;
                    entry.Length = length;
                    return true;
                }
            }

            return false;
        }

        private void Grow()
        {
            var oldTable = _table;
            var oldTotal = _bucketCount * _bucketSize;
            var newBucketCount = _bucketCount * 2;
            var newTotal = newBucketCount * _bucketSize;
            var newTable = RentClearedTable(newTotal);

            _bucketCount = newBucketCount;
            _table = newTable;
            _clearFullTable = true;
            _writtenCount = 0;

            try
            {
                for (var i = 0; i < oldTotal; i++)
                {
                    var entry = oldTable[i];

                    if (entry.Index != 0)
                    {
                        // a bucket of the doubled table receives the entries of one old bucket
                        var inserted = TryInsert(entry.Hash, entry.Index - 1, entry.Location, entry.Length);
                        Debug.Assert(inserted, "A grown bucket holds at most the entries of one old bucket.");
                    }
                }
            }
            finally
            {
                ArrayPool<Entry>.Shared.Return(oldTable);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int totalSize)
        {
            var newTable = RentClearedTable(totalSize);
            var oldTable = _table;
            _table = newTable;
            ArrayPool<Entry>.Shared.Return(oldTable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordWrittenSlot(int slot)
        {
            if (_clearFullTable)
            {
                return;
            }

            if (_writtenCount == TrackedSlotCapacity)
            {
                _clearFullTable = true;
                _writtenCount = 0;
                return;
            }

            if (_writtenSlots.Length == 0)
            {
                RentWrittenSlots();
            }

            _writtenSlots[_writtenCount++] = slot;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RentWrittenSlots()
            => _writtenSlots = ArrayPool<int>.Shared.Rent(TrackedSlotCapacity);

        private void ClearPreviousEntries()
        {
            if (_clearFullTable)
            {
                _table.AsSpan(0, _bucketCount * _bucketSize).Clear();
            }
            else
            {
                for (var i = 0; i < _writtenCount; i++)
                {
                    _table[_writtenSlots[i]] = default;
                }
            }

            _count = 0;
            ResetClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetClearState()
        {
            _writtenCount = 0;
            _clearFullTable = false;
        }

        private static Entry[] RentClearedTable(int minimumLength)
        {
            var table = ArrayPool<Entry>.Shared.Rent(minimumLength);
            table.AsSpan().Clear();
            return table;
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
