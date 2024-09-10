namespace HotChocolate.Utilities.StreamAdapters;

internal sealed class AsyncEnumerableStreamAdapter<T> : IAsyncEnumerable<object?>
{
    private readonly IAsyncEnumerable<T> _stream;

    public AsyncEnumerableStreamAdapter(IAsyncEnumerable<T> stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public async IAsyncEnumerator<object?> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in _stream.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return item;
        }
    }
}
