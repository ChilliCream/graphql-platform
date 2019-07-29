using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Execution;
using HotChocolate.Execution.Batching;
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
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    /// <summary>
    /// A base <c>GraphQL</c> query middleware.
    /// </summary>
    public abstract class QueryMiddlewareBase
#if ASPNETCLASSIC
        : RequestDelegate
#endif
    {
        private const int _badRequest = 400;
        private const int _ok = 200;

        private readonly IQueryResultSerializer _resultSerializer;
        private readonly Func<HttpContext, bool> _isPathValid;

        private IQueryRequestInterceptor<HttpContext> _interceptor;
        private bool _interceptorInitialized = false;

#if ASPNETCLASSIC
        private OwinContextAccessor _accessor;
#endif

        /// <summary>
        /// Instantiates the base query middleware with an optional pointer to
        /// the next component.
        /// </summary>
        /// <param name="next">
        /// An optional pointer to the next component.
        /// </param>
        /// <param name="queryExecutor">
        /// A required query executor resolver.
        /// </param>
        /// <param name="resultSerializer">
        /// </param>
        /// <param name="options">
        /// </param>
        protected QueryMiddlewareBase(
            RequestDelegate next,
            IQueryResultSerializer resultSerializer,
            QueryMiddlewareOptions options)
#if ASPNETCLASSIC
                : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            _resultSerializer = resultSerializer
                ?? throw new ArgumentNullException(nameof(resultSerializer));
            Options = options ??
                throw new ArgumentNullException(nameof(options));

            if (Options.Path.Value.Length > 1)
            {
                var path1 = new PathString(options.Path.Value.TrimEnd('/'));
                PathString path2 = path1.Add(new PathString("/"));
                _isPathValid = ctx => ctx.IsValidPath(path1, path2);
            }
            else
            {
                _isPathValid = ctx => ctx.IsValidPath(options.Path);
            }

#if ASPNETCLASSIC
            _accessor = queryExecutor.Schema.Services
                .GetService<IOwinContextAccessor>()
                as OwinContextAccessor;
#endif
        }

#if !ASPNETCLASSIC
        protected RequestDelegate Next { get; }
#endif

        /// <summary>
        /// Gets the GraphQL middleware options.
        /// </summary>
        protected QueryMiddlewareOptions Options { get; }


#if ASPNETCLASSIC
        /// <inheritdoc />
        public override async Task Invoke(HttpContext context)
#else
        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
#endif
        {
            if (_isPathValid(context) && CanHandleRequest(context))
            {
                try
                {
                    await HandleRequestAsync(context)
                        .ConfigureAwait(false);
                }
                catch (NotSupportedException)
                {
                    context.Response.StatusCode = _badRequest;
                }
            }
            else if (Next != null)
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks whether the request can be handled by the concrete query
        /// middleware.
        /// </summary>
        /// <param name="context">An OWIN context.</param>
        /// <returns>
        /// A value indicating whether the request can be handled.
        /// </returns>
        protected abstract bool CanHandleRequest(HttpContext context);

#if ASPNETCLASSIC
        protected static T GetService<T>(HttpContext context)
        {
            if (context.Environment.TryGetValue(
                EnvironmentKeys.ServiceProvider,
                out var value) && value is IServiceProvider serviceProvider)
            {
                return (T)serviceProvider.GetService(typeof(T));
            }

            return default;
        }
#else
        protected static T GetService<T>(HttpContext context) =>
            (T)context.RequestServices.GetService(typeof(T));
#endif

        private async Task HandleRequestAsync(
            HttpContext context)
        {
#if ASPNETCLASSIC
            if (_accessor != null)
            {
                _accessor.OwinContext = context;
            }

            using (IServiceScope serviceScope =
                Executor.Schema.Services.CreateScope())
            {
                IServiceProvider services =
                    context.CreateRequestServices(
                        serviceScope.ServiceProvider);
#else
            IServiceProvider services = context.RequestServices;
#endif

            await ExecuteRequestAsync(context, services)
                .ConfigureAwait(false);

#if ASPNETCLASSIC
            }
#endif
        }

        protected abstract Task ExecuteRequestAsync(
            HttpContext context,
            IServiceProvider services);

        protected async Task<IReadOnlyQueryRequest> BuildRequestAsync(
            HttpContext context,
            IServiceProvider services,
            IQueryRequestBuilder builder)
        {
            if (!_interceptorInitialized)
            {
                _interceptor =
                    GetService<IQueryRequestInterceptor<HttpContext>>(context);
                _interceptorInitialized = true;
            }

            if (_interceptor != null)
            {
                await _interceptor.OnCreateAsync(
                    context,
                    builder,
                    context.GetCancellationToken())
                    .ConfigureAwait(false);
            }

            builder.SetServices(services);
            builder.TryAddProperty(nameof(HttpContext), context);
            builder.TryAddProperty(nameof(ClaimsPrincipal), context.GetUser());

            if (context.IsTracingEnabled())
            {
                builder.TryAddProperty(ContextDataKeys.EnableTracing, true);
            }

            return builder.Create();
        }

        protected Task WriteResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            SetResponseHeaders(response);
            return WriteBatchResponseAsync(response, executionResult);
        }

        protected async Task WriteBatchResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            if (executionResult is IReadOnlyQueryResult queryResult)
            {
                await _resultSerializer.SerializeAsync(
                    queryResult, response.Body)
                    .ConfigureAwait(false);
            }
        }

        protected void SetResponseHeaders(
            HttpResponse response)
        {
            response.ContentType = ContentType.Json;
            response.StatusCode = _ok;
        }
    }
}
