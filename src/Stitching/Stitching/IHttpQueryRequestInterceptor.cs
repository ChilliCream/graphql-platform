using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IHttpQueryRequestInterceptor
    {
        Task OnResponseReceivedAsync(
            IReadOnlyQueryRequest request,
            HttpResponseMessage response,
            IQueryResult result);
    }
}
