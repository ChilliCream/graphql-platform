using System;
using System.Collections.Generic;
using HotChocolate.Properties;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.Execution;

public sealed class ResponseStream : ExecutionResult, IResponseStream
{
    private readonly Func<IAsyncEnumerable<IOperationResult>>? _resultStreamFactory;
    private bool _isRead;

    public ResponseStream(
        Func<IAsyncEnumerable<IOperationResult>>? resultStreamFactory,
        ExecutionResultKind kind = SubscriptionResult,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        _resultStreamFactory = resultStreamFactory ??
            throw new ArgumentNullException(nameof(resultStreamFactory));

        if (kind is not BatchResult and not DeferredResult and not SubscriptionResult)
        {
            throw new ArgumentException(
                AbstractionResources.ResponseStream_InvalidResultKind);
        }

        Kind = kind;
        ContextData = contextData;
    }

    public override ExecutionResultKind Kind { get; }

    public override IReadOnlyDictionary<string, object?>? ContextData { get; }

    public IAsyncEnumerable<IOperationResult> ReadResultsAsync()
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

        EnsureNotDisposed();

        _isRead = true;
        return _resultStreamFactory();
    }
}
