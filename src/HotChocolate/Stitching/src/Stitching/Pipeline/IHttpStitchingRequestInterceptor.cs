using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IHttpStitchingRequestInterceptor
    {
        ValueTask OnCreateRequestAsync(
            NameString targetSchema,
            IQueryRequest request,
            HttpRequestMessage requestMessage,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryResult> OnReceivedResultAsync(
            NameString targetSchema,
            IQueryRequest request,
            IQueryResult result,
            HttpResponseMessage responseMessage,
            CancellationToken cancellationToken = default);
    }
}
