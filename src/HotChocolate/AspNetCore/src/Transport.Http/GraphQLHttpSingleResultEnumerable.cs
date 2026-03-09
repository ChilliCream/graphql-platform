#if FUSION
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

#if FUSION
internal sealed class GraphQLHttpSingleResultEnumerable : IAsyncEnumerable<SourceResultDocument>
#else
internal sealed class GraphQLHttpSingleResultEnumerable : IAsyncEnumerable<OperationResult>
#endif
{
#if FUSION
    private readonly Func<CancellationToken, ValueTask<SourceResultDocument>> _singleResult;
#else
    private readonly Func<CancellationToken, ValueTask<OperationResult>> _singleResult;
#endif

#if FUSION
    public GraphQLHttpSingleResultEnumerable(Func<CancellationToken, ValueTask<SourceResultDocument>> singleResult)
#else
    public GraphQLHttpSingleResultEnumerable(Func<CancellationToken, ValueTask<OperationResult>> singleResult)
#endif
    {
        ArgumentNullException.ThrowIfNull(singleResult);

        _singleResult = singleResult;
    }

#if FUSION
    public async IAsyncEnumerator<SourceResultDocument> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#else
    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#endif
    {
        yield return await _singleResult(cancellationToken).ConfigureAwait(false);
    }
}
