using HotChocolate.Buffers;
using HotChocolate.Language;
using System.Text.Json;

namespace HotChocolate.Transport.Formatters;

internal sealed class OperationResultFormatterContext : IDisposable
{
    private Dictionary<int, PendingResultState>? _pendingResults;
    private Dictionary<Path, CachedJsonValue>? _cachedDataByPath;
    private PooledArrayWriter? _cacheBuffer;
    private PooledArrayWriter? _scratchBuffer;
    private DeferSelectionLookup? _deferSelectionLookup;
    private bool _disposed;

    public Dictionary<int, PendingResultState> PendingResults
        => _pendingResults ??= [];

    public Dictionary<Path, CachedJsonValue> CachedDataByPath
        => _cachedDataByPath ??= [];

    public PooledArrayWriter CacheBuffer
        => _cacheBuffer ??= new();

    public PooledArrayWriter ScratchBuffer
        => _scratchBuffer ??= new();

    public DeferSelectionLookup? DeferSelectionLookup
        => _deferSelectionLookup;

    public void InitializeDocument(DocumentNode? document)
    {
        if (_deferSelectionLookup is not null || document is null)
        {
            return;
        }

        _deferSelectionLookup = DeferSelectionLookup.Create(document);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cacheBuffer?.Dispose();
        _cacheBuffer = null;
        _scratchBuffer?.Dispose();
        _scratchBuffer = null;
        _disposed = true;
    }
}

internal readonly record struct PendingResultState(Path? Path, string? Label);

internal readonly record struct CachedJsonValue(
    ReadOnlyMemorySegment Segment,
    JsonValueKind ValueKind);
