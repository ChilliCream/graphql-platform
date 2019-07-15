using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Server;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using HttpResponse = Microsoft.Owin.IOwinResponse;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Interceptors
#else
namespace HotChocolate.AspNetCore.Interceptors
#endif
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
