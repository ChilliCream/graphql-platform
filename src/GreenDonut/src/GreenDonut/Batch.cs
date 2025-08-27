using System.Diagnostics;

namespace GreenDonut;

internal sealed class Batch<TKey> : Batch where TKey : notnull
{
    private const int Enqueued = 1;
    private const int Touched = 2;

    private readonly List<TKey> _keys = [];
    private readonly Dictionary<TKey, IPromise> _items = [];
    private Func<Batch<TKey>, CancellationToken, ValueTask> _dispatch = null!;
    private CancellationToken _ct = CancellationToken.None;
    private int _status = Enqueued;
    private long _timestamp;

    public bool IsScheduled { get; set; }

    public IReadOnlyList<TKey> Keys => _keys;

    public override int Size => _keys.Count;

    public override BatchStatus Status => (BatchStatus)_status;

    public override long ModifiedTimestamp => _timestamp;

    public override bool Touch()
    {
        var previous = Interlocked.Exchange(ref _status, Touched);
        return previous == Touched;
    }

    public Promise<TValue> GetOrCreatePromise<TValue>(TKey key, bool allowCachePropagation)
    {
        // we mark the batch as enqueued even if we did not really enqueued something.
        // as long as there are components interacting with this batch its good to
        // keep it in enqueued state.
        Interlocked.Exchange(ref _status, Enqueued);
        _timestamp = Stopwatch.GetTimestamp();

        if (_items.TryGetValue(key, out var value))
        {
            return (Promise<TValue>)value;
        }

        var promise = Promise<TValue>.Create(!allowCachePropagation);

        _keys.Add(key);
        _items.Add(key, promise);

        return promise;
    }

    public Promise<TValue> GetPromise<TValue>(TKey key)
        => (Promise<TValue>)_items[key];

    public override async Task DispatchAsync()
        => await _dispatch(this, _ct);

    internal void Initialize(Func<Batch<TKey>, CancellationToken, ValueTask> dispatch, CancellationToken ct)
    {
        _status = Enqueued;
        _dispatch = dispatch;
        _ct = ct;
    }

    internal void ClearUnsafe()
    {
        _keys.Clear();
        _items.Clear();
        IsScheduled = false;
        _status = Enqueued;
        _dispatch = null!;
        _ct = CancellationToken.None;
    }
}
