using HotChocolate.Execution;

namespace HotChocolate.Utilities.Subscriptions;

internal sealed class SourceStreamWrapper : ISourceStream
{
    private readonly IAsyncEnumerable<object> _stream;

    public SourceStreamWrapper(IAsyncEnumerable<object> stream)
    {
        _stream = stream;
    }

    public IAsyncEnumerable<object> ReadEventsAsync() => _stream;

    public ValueTask DisposeAsync() => default;
}
