using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IHttpQueryRequestInterceptor
    {
        Task<IReadOnlyQueryResult> OnResponseReceivedAsync(
            IReadOnlyQueryRequest request,
            HttpResponseMessage response,
            IReadOnlyQueryResult result);
    }
}
