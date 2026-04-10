using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    /// <summary>
    /// Initializes the <see cref="FetchResultStore"/> for a new request.
    /// </summary>
    public void Initialize(
        ISchemaDefinition schema,
        IErrorHandler errorHandler,
        Operation operation,
        ErrorHandlingMode errorHandlingMode,
        ulong includeFlags,
        int pathSegmentLocalPoolCapacity)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operation);

        _schema = schema;
        _errorHandler = errorHandler;
        _operation = operation;
        _errorHandlingMode = errorHandlingMode;
        _includeFlags = includeFlags;
        _disposed = false;

        _pathPool ??= new PathSegmentLocalPool(pathSegmentLocalPoolCapacity);
        _result = new CompositeResultDocument(operation, includeFlags, _pathPool);

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

        _result = new CompositeResultDocument(_operation, _includeFlags, _pathPool);
        _errors?.Clear();
        _pocketedErrors?.Clear();

        _valueCompletion = new ValueCompletion(
            this,
            _schema,
            _errorHandler,
            _errorHandlingMode,
            maxDepth: 32);

        _memory.Push(_result);
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

        // return path segments to global pool and reset local pool
        _pathPool.Dispose();
        _pathPool = null!;

        // clear errors
        _errors?.Clear();
        _pocketedErrors?.Clear();

        // reset variable writer (returns excess chunks, keeps the first)
        _variableWriter.Clean();

        // clear collect target arrays to unroot CompositeResultDocument references;
        // if they grew too large during a burst, swap them for smaller ones.
        TrimOrClearBuffer(ref _collectTargetA, maxCollectTargetRetainLength);
        TrimOrClearBuffer(ref _collectTargetB, maxCollectTargetRetainLength);
        TrimOrClearBuffer(ref _collectTargetCombined, maxCollectTargetRetainLength);

        // clear dictionaries/hashsets; drop oversized ones.
        TrimOrClear(ref _seenPaths, maxDictionaryRetainCapacity, ReferenceEqualityComparer.Instance);
        _variableDedupTable.Clear();

        // null out per-request references
        _result = default!;
        _valueCompletion = default!;
        _schema = default!;
        _errorHandler = default!;
        _operation = default!;
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
