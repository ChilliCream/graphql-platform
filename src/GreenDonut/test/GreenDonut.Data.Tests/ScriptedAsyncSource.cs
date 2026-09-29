namespace GreenDonut.Data;

/// <summary>
/// A scripted <see cref="IAsyncEnumerable{T}"/> for unit tests. Yields a fixed sequence of items,
/// can pause before a given item until a test releases its gate, can throw instead of yielding a
/// given item, and counts how often it was advanced and disposed.
/// </summary>
/// <typeparam name="T">
/// The type of the yielded items.
/// </typeparam>
public sealed class ScriptedAsyncSource<T> : IAsyncEnumerable<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly Dictionary<int, TaskCompletionSource> _gates = [];
    private readonly List<T> _yielded = [];
    private int? _throwAtIndex;
    private Exception? _exceptionToThrow;
    private Exception? _disposeException;

    public ScriptedAsyncSource(params T[] items) : this((IEnumerable<T>)items)
    {
    }

    public ScriptedAsyncSource(IEnumerable<T> items) => _items = [.. items];

    /// <summary>
    /// The number of times an enumerator of this source advanced with <c>MoveNextAsync</c>.
    /// </summary>
    public int MoveNextCount { get; private set; }

    /// <summary>
    /// The number of times an enumerator of this source was disposed.
    /// </summary>
    public int DisposeCount { get; private set; }

    /// <summary>
    /// The items yielded so far, in the order they were yielded.
    /// </summary>
    public IReadOnlyList<T> Yielded => _yielded;

    /// <summary>
    /// Pauses the enumerator before it yields the item at <paramref name="index"/> until the
    /// returned <see cref="TaskCompletionSource"/> completes.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the item to gate.
    /// </param>
    public TaskCompletionSource GateBeforeItem(int index)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _gates[index] = gate;
        return gate;
    }

    /// <summary>
    /// Makes the enumerator throw <paramref name="exception"/> instead of yielding the item at
    /// <paramref name="index"/>.
    /// </summary>
    /// <param name="index">
    /// The zero-based index at which to throw.
    /// </param>
    /// <param name="exception">
    /// The exception the enumerator throws when it reaches <paramref name="index"/>.
    /// </param>
    public void ThrowAt(int index, Exception exception)
    {
        _throwAtIndex = index;
        _exceptionToThrow = exception;
    }

    /// <summary>
    /// Makes every enumerator's <c>DisposeAsync</c> throw <paramref name="exception"/> after
    /// still counting the disposal.
    /// </summary>
    /// <param name="exception">
    /// The exception an enumerator throws when disposed.
    /// </param>
    public void ThrowOnDispose(Exception exception) => _disposeException = exception;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(this, cancellationToken);

    private sealed class Enumerator(ScriptedAsyncSource<T> source, CancellationToken cancellationToken)
        : IAsyncEnumerator<T>
    {
        private int _index = -1;

        public T Current { get; private set; } = default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            source.MoveNextCount++;
            _index++;

            if (source._gates.TryGetValue(_index, out var gate))
            {
                await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (source._throwAtIndex == _index)
            {
                throw source._exceptionToThrow!;
            }

            if (_index >= source._items.Count)
            {
                return false;
            }

            Current = source._items[_index];
            source._yielded.Add(Current);
            return true;
        }

        public ValueTask DisposeAsync()
        {
            source.DisposeCount++;

            if (source._disposeException is { } exception)
            {
                throw exception;
            }

            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// An <see cref="IAsyncDisposable"/> for unit tests that counts how often it was disposed, for
/// asserting the release of a lifetime object passed to the paging APIs.
/// </summary>
public sealed class ScriptedAsyncDisposable : IAsyncDisposable
{
    private Exception? _disposeException;

    /// <summary>
    /// The number of times this instance was disposed.
    /// </summary>
    public int DisposeCount { get; private set; }

    /// <summary>
    /// Makes <c>DisposeAsync</c> throw <paramref name="exception"/> after still counting the
    /// disposal.
    /// </summary>
    /// <param name="exception">
    /// The exception thrown when this instance is disposed.
    /// </param>
    public void ThrowOnDispose(Exception exception) => _disposeException = exception;

    public ValueTask DisposeAsync()
    {
        DisposeCount++;

        if (_disposeException is { } exception)
        {
            throw exception;
        }

        return ValueTask.CompletedTask;
    }
}
