using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    internal sealed class DelegateHttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        private readonly HttpRequestInterceptorDelegate _interceptor;

        public DelegateHttpRequestInterceptor(HttpRequestInterceptorDelegate interceptor)
        {
            _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        }

        public override async ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            await _interceptor(context, requestExecutor, requestBuilder, cancellationToken);
            await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
