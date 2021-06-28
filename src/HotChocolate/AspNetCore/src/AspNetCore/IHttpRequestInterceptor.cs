using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public interface IHttpRequestInterceptor
    {
        ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken);
    }
}
