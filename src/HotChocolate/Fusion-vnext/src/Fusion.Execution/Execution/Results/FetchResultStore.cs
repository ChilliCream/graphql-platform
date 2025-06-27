using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
internal sealed class FetchResultStore : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ResultPoolSession _resultPoolSession;
    private readonly ValueCompletion _valueCompletion;
    private readonly Operation _operation;
    private readonly ObjectResult _root;
    private readonly ulong _includeFlags;
    private readonly ImmutableArray<IError> _errors = [];
    private bool _isInitialized;

    public FetchResultStore(
        ISchemaDefinition schema,
        ResultPoolSession resultPoolSession,
        Operation operation,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(operation);

        _resultPoolSession = resultPoolSession;
        _valueCompletion = new ValueCompletion(schema, resultPoolSession, ErrorHandling.Propagate, 32, includeFlags);
        _operation = operation;
        _root = resultPoolSession.RentObjectResult();
        _includeFlags = includeFlags;
    }

    public ObjectResult Data => _root;

    public ImmutableArray<IError> Errors => _errors;

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

                    if (!_isInitialized)
                    {
                        _root.Initialize(_resultPoolSession, selectionSet, _includeFlags);
                        _isInitialized = true;
                    }

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
        ImmutableArray<OperationRequirement> requiredData)
    {
        _lock.EnterReadLock();

        try
        {
            // TODO: walk `_root` and build variable sets once implemented
            return [];
        }
        finally
        {
            _lock.ExitReadLock();
        }
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
    public void Dispose()
    {
        _lock.Dispose();
    }
}
