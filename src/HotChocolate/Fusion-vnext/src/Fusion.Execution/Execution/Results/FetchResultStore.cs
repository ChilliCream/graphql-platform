using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
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
using HotChocolate.Text.Json;
using HotChocolate.Transport.Http;
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

    public VariableValueSets CreateVariableValueSets(
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
                return VariableValueSets.Empty;
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
    public VariableValueSets CreateVariableValueSets(
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
                return VariableValueSets.Empty;
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

    private VariableValueSets BuildVariableValueSets(
        List<CompositeResultElement> elements,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        var buffer = new PooledArrayWriter();
        using var candidateBuffer = new PooledArrayWriter();
        var candidateWriter = new JsonWriter(candidateBuffer, new JsonWriterOptions());
        var fileMapVariables = ContainsFileReference(requestVariables)
            ? new ObjectValueNode(requestVariables)
            : null;

        VariableValues[]? variableValueSets = null;
        Dictionary<ulong, List<int>>? seen = null;
        List<Path>?[]? additionalPaths = null;
        var nextIndex = 0;

        try
        {
            foreach (var result in elements)
            {
                candidateBuffer.Reset();
                candidateWriter.Reset(candidateBuffer);
                candidateWriter.WriteStartObject();

                for (var i = 0; i < requestVariables.Count; i++)
                {
                    var field = requestVariables[i];
                    candidateWriter.WritePropertyName(field.Name.Value);
                    WriteValueNode(candidateWriter, field.Value);
                }

                var shouldSkip = false;

                for (var i = 0; i < requiredData.Length; i++)
                {
                    var requirement = requiredData[i];
                    candidateWriter.WritePropertyName(requirement.Key);

                    var status = WriteMappedValue(candidateWriter, result, requirement.Map);

                    if (status is MappingStatus.Missing
                        || status is MappingStatus.Null
                            && requirement.Type.Kind == SyntaxKind.NonNullType)
                    {
                        shouldSkip = true;
                        break;
                    }
                }

                if (shouldSkip)
                {
                    continue;
                }

                candidateWriter.WriteEndObject();

                var candidate = candidateBuffer.WrittenSpan;
                var hash = XxHash3.HashToUInt64(candidate);

                if (TryGetExistingIndex(hash, candidate, variableValueSets, seen, out var existingIndex))
                {
                    additionalPaths ??= new List<Path>?[elements.Count];
                    (additionalPaths[existingIndex] ??= []).Add(result.Path);
                    continue;
                }

                variableValueSets ??= new VariableValues[elements.Count];

                var start = buffer.Length;
                candidate.CopyTo(buffer.GetSpan(candidate.Length));
                buffer.Advance(candidate.Length);

                variableValueSets[nextIndex] = new VariableValues(
                    result.Path,
                    buffer.GetWrittenMemorySegment(start, candidate.Length),
                    fileMapVariables);

                seen ??= [];

                if (!seen.TryGetValue(hash, out var bucket))
                {
                    bucket = [];
                    seen.Add(hash, bucket);
                }

                bucket.Add(nextIndex);
                nextIndex++;
            }

            var values = FinalizeVariableValueSets(variableValueSets, additionalPaths, nextIndex);

            if (values.IsEmpty)
            {
                buffer.Dispose();
                return VariableValueSets.Empty;
            }

            return new VariableValueSets(values, buffer);
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    private static bool TryGetExistingIndex(
        ulong hash,
        ReadOnlySpan<byte> candidate,
        VariableValues[]? variableValueSets,
        Dictionary<ulong, List<int>>? seen,
        out int existingIndex)
    {
        if (variableValueSets is not null
            && seen is not null
            && seen.TryGetValue(hash, out var bucket))
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                var index = bucket[i];

                if (variableValueSets[index].Variables.Span.SequenceEqual(candidate))
                {
                    existingIndex = index;
                    return true;
                }
            }
        }

        existingIndex = -1;
        return false;
    }

    private MappingStatus WriteMappedValue(
        JsonWriter writer,
        CompositeResultElement result,
        IValueSelectionNode node)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    if (EvaluateMappedValue(result, branch) is not MappingStatus.Missing)
                    {
                        return WriteMappedValue(writer, result, branch);
                    }
                }

                return MappingStatus.Missing;

            case PathNode path:
                var resolvedPath = ResolvePath(result, path);

                if (resolvedPath.ValueKind is JsonValueKind.Undefined)
                {
                    return MappingStatus.Missing;
                }

                if (resolvedPath.ValueKind is JsonValueKind.Null)
                {
                    writer.WriteNullValue();
                    return MappingStatus.Null;
                }

                // Note: to capture data from the introspection
                // system we would need to also cover raw field results.
                if (resolvedPath.Selection is { IsLeaf: true })
                {
                    WriteCompositeValue(writer, resolvedPath);
                    return MappingStatus.Written;
                }

                throw new InvalidSelectionMapPathException(path);

            case ObjectValueSelectionNode objectValue:
                if (result.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Only object results are supported.");
                }

                writer.WriteStartObject();

                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);

                    var status = field.ValueSelection is null
                        ? WriteFieldSelection(writer, result, field.Name.Value)
                        : WriteMappedValue(writer, result, field.ValueSelection);

                    if (status is MappingStatus.Missing)
                    {
                        return MappingStatus.Missing;
                    }
                }

                writer.WriteEndObject();
                return MappingStatus.Written;

            case PathObjectValueSelectionNode pathObjectValue:
                var resolvedObjectPath = ResolvePath(result, pathObjectValue.Path);
                var valueKind = resolvedObjectPath.ValueKind;

                if (valueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    return MappingStatus.Missing;
                }

                if (valueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Only object results are supported.");
                }

                return WriteMappedValue(
                    writer,
                    resolvedObjectPath,
                    pathObjectValue.ObjectValueSelection);

            case ListValueSelectionNode listValue:
                if (result.ValueKind is not JsonValueKind.Array)
                {
                    return MappingStatus.Missing;
                }

                writer.WriteStartArray();

                foreach (var item in result.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Null)
                    {
                        writer.WriteNullValue();
                        continue;
                    }

                    var status = WriteMappedValue(writer, item, listValue.ElementSelection);

                    if (status is MappingStatus.Missing)
                    {
                        return MappingStatus.Missing;
                    }
                }

                writer.WriteEndArray();
                return MappingStatus.Written;

            case PathListValueSelectionNode pathListValue:
                var resolvedListPath = ResolvePath(result, pathListValue.Path);
                var resolvedListKind = resolvedListPath.ValueKind;

                return resolvedListKind switch
                {
                    JsonValueKind.Undefined => MappingStatus.Missing,
                    JsonValueKind.Null => WriteNull(writer),
                    JsonValueKind.Array => WriteMappedValue(
                        writer,
                        resolvedListPath,
                        pathListValue.ListValueSelection),
                    _ => MappingStatus.Missing
                };

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    private MappingStatus EvaluateMappedValue(
        CompositeResultElement result,
        IValueSelectionNode node)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    var status = EvaluateMappedValue(result, branch);

                    if (status is not MappingStatus.Missing)
                    {
                        return status;
                    }
                }

                return MappingStatus.Missing;

            case PathNode path:
                var resolvedPath = ResolvePath(result, path);

                if (resolvedPath.ValueKind is JsonValueKind.Undefined)
                {
                    return MappingStatus.Missing;
                }

                if (resolvedPath.ValueKind is JsonValueKind.Null)
                {
                    return MappingStatus.Null;
                }

                // Note: to capture data from the introspection
                // system we would need to also cover raw field results.
                if (resolvedPath.Selection is { IsLeaf: true })
                {
                    return MappingStatus.Written;
                }

                throw new InvalidSelectionMapPathException(path);

            case ObjectValueSelectionNode objectValue:
                if (result.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Only object results are supported.");
                }

                foreach (var field in objectValue.Fields)
                {
                    var status = field.ValueSelection is null
                        ? EvaluateFieldSelection(result, field.Name.Value)
                        : EvaluateMappedValue(result, field.ValueSelection);

                    if (status is MappingStatus.Missing)
                    {
                        return MappingStatus.Missing;
                    }
                }

                return MappingStatus.Written;

            case PathObjectValueSelectionNode pathObjectValue:
                var resolvedObjectPath = ResolvePath(result, pathObjectValue.Path);
                var valueKind = resolvedObjectPath.ValueKind;

                if (valueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    return MappingStatus.Missing;
                }

                if (valueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Only object results are supported.");
                }

                return EvaluateMappedValue(resolvedObjectPath, pathObjectValue.ObjectValueSelection);

            case ListValueSelectionNode listValue:
                if (result.ValueKind is not JsonValueKind.Array)
                {
                    return MappingStatus.Missing;
                }

                foreach (var item in result.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Null)
                    {
                        continue;
                    }

                    if (EvaluateMappedValue(item, listValue.ElementSelection) is MappingStatus.Missing)
                    {
                        return MappingStatus.Missing;
                    }
                }

                return MappingStatus.Written;

            case PathListValueSelectionNode pathListValue:
                var resolvedListPath = ResolvePath(result, pathListValue.Path);
                var resolvedListKind = resolvedListPath.ValueKind;

                return resolvedListKind switch
                {
                    JsonValueKind.Undefined => MappingStatus.Missing,
                    JsonValueKind.Null => MappingStatus.Null,
                    JsonValueKind.Array => EvaluateMappedValue(
                        resolvedListPath,
                        pathListValue.ListValueSelection),
                    _ => MappingStatus.Missing
                };

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MappingStatus WriteNull(JsonWriter writer)
    {
        writer.WriteNullValue();
        return MappingStatus.Null;
    }

    private static MappingStatus EvaluateFieldSelection(
        CompositeResultElement result,
        string fieldName)
    {
        if (!result.TryGetProperty(fieldName, out var fieldResult)
            || fieldResult.ValueKind is JsonValueKind.Undefined)
        {
            return MappingStatus.Missing;
        }

        return fieldResult.ValueKind is JsonValueKind.Null
            ? MappingStatus.Null
            : MappingStatus.Written;
    }

    private static MappingStatus WriteFieldSelection(
        JsonWriter writer,
        CompositeResultElement result,
        string fieldName)
    {
        if (!result.TryGetProperty(fieldName, out var fieldResult)
            || fieldResult.ValueKind is JsonValueKind.Undefined)
        {
            return MappingStatus.Missing;
        }

        if (fieldResult.ValueKind is JsonValueKind.Null)
        {
            writer.WriteNullValue();
            return MappingStatus.Null;
        }

        WriteCompositeValue(writer, fieldResult);
        return MappingStatus.Written;
    }

    private static void WriteCompositeValue(
        JsonWriter writer,
        CompositeResultElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                foreach (var property in value.EnumerateObject())
                {
                    writer.WritePropertyName(property.NameSpan);
                    WriteCompositeValue(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in value.EnumerateArray())
                {
                    WriteCompositeValue(writer, item);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(value.AssertString());
                break;

            case JsonValueKind.Number:
                writer.WriteNumberValue(value.GetRawValue(includeQuotes: false));
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new NotSupportedException(
                    $"The JSON value kind '{value.ValueKind}' is not supported.");
        }
    }

    private CompositeResultElement ResolvePath(
        CompositeResultElement result,
        PathNode path)
    {
        if (result.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        if (path.TypeName is not null)
        {
            var type = _schema.Types.GetType<IOutputTypeDefinition>(path.TypeName.Value);

            if (!type.IsAssignableFrom(result.AssertSelectionSet().Type))
            {
                return default;
            }
        }

        var currentSegment = path.PathSegment;
        var currentResult = result;
        var currentValueKind = result.ValueKind;

        while (currentSegment is not null
            && currentValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            if (!currentResult.TryGetProperty(currentSegment.FieldName.Value, out var fieldResult))
            {
                return default;
            }

            var fieldResultValueKind = fieldResult.ValueKind;

            if (fieldResultValueKind is JsonValueKind.Null)
            {
                return fieldResult;
            }

            if (currentSegment.TypeName is not null)
            {
                if (fieldResultValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = fieldResult;
                currentValueKind = fieldResultValueKind;

                var type = _schema.Types.GetType<IOutputTypeDefinition>(currentSegment.TypeName.Value);

                if (!type.IsAssignableFrom(currentResult.AssertSelectionSet().Type))
                {
                    return default;
                }

                currentSegment = currentSegment.PathSegment;
                continue;
            }

            if (currentSegment.PathSegment is not null)
            {
                if (fieldResultValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = fieldResult;
                currentSegment = currentSegment.PathSegment;
                continue;
            }

            return fieldResult;
        }

        return currentResult;
    }

    private static void WriteValueNode(JsonWriter writer, IValueNode value)
    {
        switch (value)
        {
            case ObjectValueNode objectValue:
                writer.WriteStartObject();

                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);
                    WriteValueNode(writer, field.Value);
                }

                writer.WriteEndObject();
                return;

            case ListValueNode listValue:
                writer.WriteStartArray();

                foreach (var item in listValue.Items)
                {
                    WriteValueNode(writer, item);
                }

                writer.WriteEndArray();
                return;

            case StringValueNode stringValue:
                writer.WriteStringValue(stringValue.AsSpan());
                return;

            case IntValueNode intValue:
                writer.WriteNumberValue(intValue.AsSpan());
                return;

            case FloatValueNode floatValue:
                writer.WriteNumberValue(floatValue.AsSpan());
                return;

            case BooleanValueNode booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                return;

            case EnumValueNode enumValue:
                writer.WriteStringValue(enumValue.AsSpan());
                return;

            case FileReferenceNode:
            case NullValueNode:
                writer.WriteNullValue();
                return;

            default:
                throw new NotSupportedException(
                    $"The value node kind '{value.Kind}' is not supported.");
        }
    }

    private static bool ContainsFileReference(IReadOnlyList<ObjectFieldNode> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (ContainsFileReference(fields[i].Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsFileReference(IValueNode value)
    {
        switch (value)
        {
            case FileReferenceNode:
                return true;

            case ObjectValueNode objectValue:
                foreach (var field in objectValue.Fields)
                {
                    if (ContainsFileReference(field.Value))
                    {
                        return true;
                    }
                }

                return false;

            case ListValueNode listValue:
                foreach (var item in listValue.Items)
                {
                    if (ContainsFileReference(item))
                    {
                        return true;
                    }
                }

                return false;

            default:
                return false;
        }
    }

    private enum MappingStatus : byte
    {
        Missing,
        Null,
        Written
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

        while (_memory.TryPop(out var memory))
        {
            memory.Dispose();
        }
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
                    var variableValueSet = variableValueSets[i];
                    variableValueSets[i] = new VariableValues(
                        variableValueSet.Path,
                        variableValueSet.Variables,
                        variableValueSet.FileMapVariables)
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
}
