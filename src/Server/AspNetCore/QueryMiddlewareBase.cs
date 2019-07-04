using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

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
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            QueryMiddlewareOptions options)
#if ASPNETCLASSIC
                : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            Executor = queryExecutor ??
                throw new ArgumentNullException(nameof(queryExecutor));
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

        /// <summary>
        /// Gets the GraphQL query executor resolver.
        /// </summary>
        protected IQueryExecutor Executor { get; }

        protected IQueryResultSerializer Serializer => _resultSerializer;

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
                    await HandleRequestAsync(context, Executor)
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

        /// <summary>
        /// Creates a new query request.
        /// </summary>
        /// <param name="context">An OWIN context.</param>
        /// <returns>A new query request.</returns>
        protected abstract Task<IQueryRequestBuilder> CreateQueryRequestAsync(
            HttpContext context);

        private async Task<IReadOnlyQueryRequest>
            CreateQueryRequestInternalAsync(
                HttpContext context,
                IServiceProvider services)
        {
            IQueryRequestBuilder builder =
                await CreateQueryRequestAsync(context)
                    .ConfigureAwait(false);

            OnCreateRequestAsync onCreateRequest = Options.OnCreateRequest
                ?? GetService<OnCreateRequestAsync>(context);

            builder.AddProperty(nameof(HttpContext), context);
            builder.AddProperty(nameof(ClaimsPrincipal), context.GetUser());
            builder.SetServices(services);

            if (context.IsTracingEnabled())
            {
                builder.AddProperty(ContextDataKeys.EnableTracing, true);
            }

            if (onCreateRequest != null)
            {
                CancellationToken requestAborted =
                    context.GetCancellationToken();

                await onCreateRequest(
                    new HttpContextWrapper(context),
                    builder,
                    requestAborted)
                    .ConfigureAwait(false);
            }

            return builder.Create();
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            IQueryExecutor queryExecutor)
        {
#if ASPNETCLASSIC
            if (_accessor != null)
            {
                _accessor.OwinContext = context;
            }

            using (IServiceScope serviceScope =
                Executor.Schema.Services.CreateScope())
            {
                IServiceProvider serviceProvider =
                    context.CreateRequestServices(
                        serviceScope.ServiceProvider);
#else
            IServiceProvider serviceProvider = context.RequestServices;
#endif

                IReadOnlyQueryRequest request =
                    await CreateQueryRequestInternalAsync(context, serviceProvider)
                        .ConfigureAwait(false);

                IExecutionResult result = await queryExecutor
                    .ExecuteAsync(request, context.GetCancellationToken())
                    .ConfigureAwait(false);

                await WriteResponseAsync(context.Response, result)
                    .ConfigureAwait(false);
#if ASPNETCLASSIC
            }
#endif
        }

        private async Task WriteResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            if (executionResult is IReadOnlyQueryResult queryResult)
            {
                response.ContentType = ContentType.Json;
                response.StatusCode = _ok;

                await _resultSerializer.SerializeAsync(
                    queryResult, response.Body)
                    .ConfigureAwait(false);
            }
        }
    }
}
