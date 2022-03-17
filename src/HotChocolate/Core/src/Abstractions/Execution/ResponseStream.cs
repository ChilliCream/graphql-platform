using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution;

public sealed class ResponseStream : ExecutionResult, IResponseStream
{
    private readonly Func<IAsyncEnumerable<IQueryResult>>? _resultStreamFactory;
    private bool _isRead;
    private bool _disposed;

    public ResponseStream(
        Func<IAsyncEnumerable<IQueryResult>>? resultStreamFactory,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        _resultStreamFactory = resultStreamFactory ??
            throw new ArgumentNullException(nameof(resultStreamFactory));
        ContextData = contextData;
    }

    public override ExecutionResultKind Kind => ExecutionResultKind.StreamResult;

    public override IReadOnlyDictionary<string, object?>? ContextData { get; }

    public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
    {
        if (_resultStreamFactory is null)
        {
            throw new InvalidOperationException(
                AbstractionResources.SubscriptionResult_ResultHasErrors);
        }

        if (_isRead)
        {
            throw new InvalidOperationException(
                AbstractionResources.SubscriptionResult_ReadOnlyOnce);
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResponseStream));
        }

        _isRead = true;
        return _resultStreamFactory();
    }
}
