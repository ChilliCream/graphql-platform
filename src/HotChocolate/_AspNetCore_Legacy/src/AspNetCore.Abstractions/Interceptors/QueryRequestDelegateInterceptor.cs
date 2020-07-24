using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Interceptors
{
    public delegate Task OnCreateRequestAsync(
        HttpContext context,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);

    public class QueryRequestDelegateInterceptor
        : IQueryRequestInterceptor<HttpContext>
    {
        private readonly OnCreateRequestAsync _interceptor;

        public QueryRequestDelegateInterceptor(
            OnCreateRequestAsync interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }
            _interceptor = interceptor;
        }

        public Task OnCreateAsync(
            HttpContext context,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            return _interceptor(context, requestBuilder, cancellationToken);
        }
    }
}
