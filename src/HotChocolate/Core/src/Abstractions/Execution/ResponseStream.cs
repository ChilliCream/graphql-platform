using System.Collections.Immutable;
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
        IReadOnlyDictionary<string, object?>? contextData = null,
        IReadOnlyList<Func<IOperationResult, IOperationResult>>? onFirstResult = null)
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
        OnFirstResult = onFirstResult ?? ImmutableArray<Func<IOperationResult, IOperationResult>>.Empty;
    }

    public override ExecutionResultKind Kind { get; }

    public override IReadOnlyDictionary<string, object?>? ContextData { get; }

    public IReadOnlyList<Func<IOperationResult, IOperationResult>> OnFirstResult { get; }

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
        return new OperationResultStream(_resultStreamFactory, ExecuteOnFirstResult);
    }

    /// <summary>
    /// Creates a new response stream with a list of mutators that are applied to the first result of this stream.
    /// </summary>
    /// <param name="onFirstResult">
    /// The mutators that are applied to the first result of this stream.
    /// </param>
    /// <returns>
    /// Returns a new response stream with the specified mutators.
    /// </returns>
    public ResponseStream WithOnFirstResult(
        IReadOnlyList<Func<IOperationResult, IOperationResult>> onFirstResult)
    {
        var newStream = new ResponseStream(
            _resultStreamFactory,
            Kind,
            ContextData,
            onFirstResult);

        foreach (var cleanupTask in CleanupTasks)
        {
            newStream.RegisterForCleanup(cleanupTask);
        }

        return newStream;
    }

    private class OperationResultStream(
        Func<IAsyncEnumerable<IOperationResult>> resultStreamFactory,
        Func<IOperationResult, IOperationResult> onFirstResult)
        : IAsyncEnumerable<IOperationResult>
    {
        public async IAsyncEnumerator<IOperationResult> GetAsyncEnumerator(
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

    private IOperationResult ExecuteOnFirstResult(IOperationResult firstResult)
    {
        foreach (var mutator in OnFirstResult)
        {
            firstResult = mutator(firstResult);
        }

        return firstResult;
    }
}
