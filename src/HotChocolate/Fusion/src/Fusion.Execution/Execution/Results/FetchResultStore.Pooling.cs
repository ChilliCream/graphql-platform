using System.Buffers;
using System.Diagnostics;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    public void Initialize(
        IMemoryArena arena,
        FusionSchemaDefinition schema,
        IErrorHandler errorHandler,
        Operation operation,
        ErrorHandlingMode errorHandlingMode,
        ConditionFlags includeFlags,
        ConditionFlags deferFlags,
        int pathSegmentLocalPoolCapacity)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operation);

        _arena = arena;
        _variableWriterHasArena = false;
        _leafKeyWriterHasArena = false;
        _schema = schema;
        _errorHandler = errorHandler;
        _operation = operation;
        _errorHandlingMode = errorHandlingMode;
        _includeFlags = includeFlags.Word0;
        _deferFlags = deferFlags.Word0;
        _wideIncludeFlags = includeFlags.Overflow;
        _wideDeferFlags = deferFlags.Overflow;
        _disposed = false;

        _pathPool ??= new PathSegmentLocalPool(pathSegmentLocalPoolCapacity);
        _result = new CompositeResultDocument(
            arena,
            operation,
            includeFlags,
            deferFlags,
            _pathPool);

        _valueCompletion = new ValueCompletion(
            this,
            _schema,
            _errorHandler,
            _errorHandlingMode,
            maxDepth: 32);

        _memory.Push(_result);
    }

    /// <summary>
    /// Resets the store for the next subscription event onto <paramref name="arena"/>.
    /// This rebuilds the pending result document and clears accumulated errors.
    /// </summary>
    public void Reset(IMemoryArena arena)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(arena);

        // arenas can only be reset in a subscription context,
        // and only once complete is called on the context.
        Debug.Assert(
            _memory.Count <= 1,
            "Reset expects an empty stack or a single pending seed result. "
            + "Extra entries mean a prior event's memory owners were not drained by Complete.");

        // The most recent seed result document is untouched at this point (no data has been added
        // for the current event yet), so it is safe to discard it and rebuild over the new arena.
        if (_memory.TryPop(out var pending))
        {
            pending.Dispose();
        }

        // variable values written before this event stay readable in the arena they were
        // written to, so the event writes its variables with a new writer
        if (_variableWriterHasArena)
        {
            _variableWriter.Dispose();
            _variableWriter = new ArenaBufferWriter();
            _variableWriterHasArena = false;
        }

        // the leaf key writer moves to the new arena on its next use
        _leafKeyWriter?.Release();
        _leafKeyWriterHasArena = false;
        _arena = arena;

        _result = new CompositeResultDocument(
            _arena,
            _operation,
            new ConditionFlags(_includeFlags, _wideIncludeFlags),
            new ConditionFlags(_deferFlags, _wideDeferFlags),
            _pathPool);

        _errors?.Clear();
        _pocketedErrors?.Clear();

        _valueCompletion = new ValueCompletion(
            this,
            _schema,
            _errorHandler,
            _errorHandlingMode,
            maxDepth: 32);

        // The arena is a disposable instance whose lifetime travels with the result it backs.
        _memory.Push((IDisposable)arena);
        _memory.Push(_result);
    }

    /// <summary>
    /// Hands every retained memory owner to <paramref name="result"/> for disposal.
    /// Result documents stop renting from the request scoped path pool once handed off.
    /// </summary>
    internal void TransferMemoryOwnersTo(IExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        foreach (var memoryOwner in _memory)
        {
            if (memoryOwner is CompositeResultDocument document)
            {
                document.DetachPathPool();
            }

            result.RegisterForCleanup(memoryOwner);
        }

        _memory.Clear();
    }

    /// <summary>
    /// Cleans the store for return to the pool.
    /// Releases per-request state while retaining reusable buffers.
    /// </summary>
    internal void Clean(int maxCollectTargetRetainLength, int maxDictionaryRetainCapacity)
    {
        // drain and dispose per-request memory
        while (_memory.TryPop(out var memory))
        {
            memory.Dispose();
        }

        // free this request's path segment arrays for the next request
        _pathPool?.Reset();

        // clear errors
        _errors?.Clear();
        _pocketedErrors?.Clear();

        // drop the arena memory of the variable and leaf key writers
        _variableWriter.Release();
        _variableWriterHasArena = false;
        _leafKeyWriter?.Release();
        _leafKeyWriterHasArena = false;

        // release the forwarded variable buffer if a large input made it grow
        if (_forwardedVariableWriter is { Capacity: > MaxRetainedForwardedVariableCapacity })
        {
            _forwardedVariableWriter.Dispose();
            _forwardedVariableWriter = null;
        }

        // clear collect target arrays to unroot CompositeResultDocument references;
        // if they grew too large during a burst, swap them for smaller ones.
        TrimOrClearBuffer(ref _collectTargetA, maxCollectTargetRetainLength);
        TrimOrClearBuffer(ref _collectTargetB, maxCollectTargetRetainLength);
        TrimOrClearBuffer(ref _collectTargetCombined, maxCollectTargetRetainLength);

        // drop dedup tables that a lookup with many targets made grow
        _variableDedupTable.Trim(maxCollectTargetRetainLength * 4);
        _leafKeyDedupTable?.Trim(maxCollectTargetRetainLength * 4);

        // drop the leaf value buffer if a wide lookup selection made it grow
        if (_leafValues.Length > maxCollectTargetRetainLength)
        {
            _leafValues = [];
        }

        // clear dictionaries/hashsets; drop oversized ones.
        TrimOrClear(ref _seenPaths, maxDictionaryRetainCapacity, ReferenceEqualityComparer.Instance);

        // null out per-request references
        _result = default!;
        _valueCompletion = default!;
        _schema = default!;
        _errorHandler = default!;
        _operation = default!;
        _arena = default!;
        _wideIncludeFlags = null;
        _wideDeferFlags = null;
    }

    private static void TrimOrClearBuffer(ref CompositeResultElement[] buffer, int maxRetainLength)
    {
        if (buffer.Length > maxRetainLength)
        {
            ArrayPool<CompositeResultElement>.Shared.Return(buffer, clearArray: true);
            buffer = ArrayPool<CompositeResultElement>.Shared.Rent(64);
        }
        else
        {
            buffer.AsSpan().Clear();
        }
    }

    private static void TrimOrClear<TKey>(
        ref HashSet<TKey> set,
        int maxRetainCapacity,
        IEqualityComparer<TKey> comparer)
    {
        if (set.Count > maxRetainCapacity)
        {
            set = new HashSet<TKey>(comparer);
        }
        else
        {
            set.Clear();
        }
    }

    private static void TrimOrClear<TKey, TValue>(
        ref Dictionary<TKey, TValue> dict,
        int maxRetainCapacity,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        if (dict.Count > maxRetainCapacity)
        {
            dict = comparer is null
                ? new Dictionary<TKey, TValue>()
                : new Dictionary<TKey, TValue>(comparer);
        }
        else
        {
            dict.Clear();
        }
    }
}
