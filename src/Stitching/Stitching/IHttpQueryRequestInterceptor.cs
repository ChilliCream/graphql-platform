using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IHttpQueryRequestInterceptor
    {
        Task OnResponseReceivedAsync(
            IHttpQueryRequest request,
            HttpResponseMessage response,
            IQueryResult result);
    }
}
