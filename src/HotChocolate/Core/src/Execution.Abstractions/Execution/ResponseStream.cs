using System.Collections.Immutable;
using static HotChocolate.Execution.ExecutionResultKind;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

public sealed class ResponseStream : ExecutionResult, IResponseStream
{
    private readonly Func<IAsyncEnumerable<OperationResult>>? _resultStreamFactory;
    private bool _isRead;

    public ResponseStream(
        Func<IAsyncEnumerable<OperationResult>>? resultStreamFactory,
        ExecutionResultKind kind = SubscriptionResult)
    {
        _resultStreamFactory = resultStreamFactory ??
            throw new ArgumentNullException(nameof(resultStreamFactory));

        if (kind is not BatchResult and not DeferredResult and not SubscriptionResult)
        {
            throw new ArgumentException(ResponseStream_InvalidResultKind);
        }

        Kind = kind;
    }

    public override ExecutionResultKind Kind { get; }

    public ImmutableList<Func<OperationResult, OperationResult>> OnFirstResult
    {
        get => Features.Get<ImmutableList<Func<OperationResult, OperationResult>>>() ?? [];
        set => Features.Set(value);
    }

    public IAsyncEnumerable<OperationResult> ReadResultsAsync()
    {
        if (_resultStreamFactory is null)
        {
            throw new InvalidOperationException(SubscriptionResult_ResultHasErrors);
        }

        if (_isRead)
        {
            throw new InvalidOperationException(SubscriptionResult_ReadOnlyOnce);
        }

        EnsureNotDisposed();

        _isRead = true;
        return new OperationResultStream(_resultStreamFactory, ExecuteOnFirstResult);
    }

    private class OperationResultStream(
        Func<IAsyncEnumerable<OperationResult>> resultStreamFactory,
        Func<OperationResult, OperationResult> onFirstResult)
        : IAsyncEnumerable<OperationResult>
    {
        public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            var first = true;

            await foreach (var result in resultStreamFactory().WithCancellation(cancellationToken))
            {
                if (first)
                {
                    yield return onFirstResult(result);
                    first = false;
                    continue;
                }

                yield return result;
            }
        }
    }

    private OperationResult ExecuteOnFirstResult(OperationResult firstResult)
    {
        foreach (var mutator in OnFirstResult)
        {
            firstResult = mutator(firstResult);
        }

        return firstResult;
    }
}
