namespace HotChocolate.Transport.Http;

internal sealed class GraphQLHttpSingleResultEnumerable : IAsyncEnumerable<OperationResult>
{
    private readonly Func<CancellationToken, ValueTask<OperationResult>> _singleResult;

    public GraphQLHttpSingleResultEnumerable(Func<CancellationToken, ValueTask<OperationResult>> singleResult)
    {
        ArgumentNullException.ThrowIfNull(singleResult);

        _singleResult = singleResult;
    }

    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        yield return await _singleResult(cancellationToken).ConfigureAwait(false);
    }
}
