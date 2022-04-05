using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

public class DefaultHttpStitchingRequestInterceptor : IHttpStitchingRequestInterceptor
{
    public virtual ValueTask OnCreateRequestAsync(
        NameString targetSchema,
        IQueryRequest request,
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken = default)
        => default;

    public virtual ValueTask<IQueryResult> OnReceivedResultAsync(
        NameString targetSchema,
        IQueryRequest request,
        IQueryResult result,
        HttpResponseMessage responseMessage,
        CancellationToken cancellationToken = default) => new(result);
}

