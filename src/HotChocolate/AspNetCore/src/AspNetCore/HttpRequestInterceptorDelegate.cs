using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public delegate ValueTask HttpRequestInterceptorDelegate(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);
}
