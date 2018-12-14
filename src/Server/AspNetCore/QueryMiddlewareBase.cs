using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;

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
        private readonly IQueryExecuter _queryExecuter;

        /// <summary>
        /// Instantiates the base query middleware with an optional pointer to
        /// the next component.
        /// </summary>
        /// <param name="next">
        /// An optional pointer to the next component.
        /// </param>
        /// <param name="queryExecuterResolver">
        /// A required query executor resolver.
        /// </param>
        /// <param name="options">
        /// </param>
        protected QueryMiddlewareBase(
            RequestDelegate next,
            IQueryExecuter queryExecuter,
            QueryMiddlewareOptions options)
#if ASPNETCLASSIC
                : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            _queryExecuter = queryExecuter ??
                throw new ArgumentNullException(nameof(queryExecuter));
            Options = options ??
                throw new ArgumentNullException(nameof(options));
            Services = Executer.Schema.Services;
        }

        /// <summary>
        /// Gets the GraphQL query executer resolver.
        /// </summary>
        protected IQueryExecuter Executer
        {
            get
            {
                return _queryExecuter;
            }
        }

#if !ASPNETCLASSIC
        protected RequestDelegate Next { get; }
#endif

        /// <summary>
        /// Gets the GraphQL middleware options.
        /// </summary>
        protected QueryMiddlewareOptions Options { get; }

        protected IServiceProvider Services { get; }


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
            if (context.IsValidPath(Options.Path) && CanHandleRequest(context))
            {
                try
                {
                    await HandleRequestAsync(context, Executer)
                        .ConfigureAwait(false);
                }
                catch (NotSupportedException)
                {
                    context.Response.StatusCode = 400;

                    return;
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
        protected T GetService<T>(HttpContext context)
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
        protected T GetService<T>(HttpContext context) =>
            (T)context.RequestServices.GetService(typeof(T));
#endif

        /// <summary>
        /// Creates a new query request.
        /// </summary>
        /// <param name="context">An OWIN context.</param>
        /// <returns>A new query request.</returns>
        protected abstract Task<QueryRequest> CreateQueryRequest(
            HttpContext context);

        private async Task<QueryRequest> CreateQueryRequestInternal(
            HttpContext context)
        {
            QueryRequest request = await CreateQueryRequest(context)
                .ConfigureAwait(false);
            OnCreateRequestAsync onCreateRequest = Options.OnCreateRequest
                ?? GetService<OnCreateRequestAsync>(context);
            var requestProperties = new Dictionary<string, object>
            {
                { nameof(ClaimsPrincipal), context.GetUser() }
            };

            request.Properties = requestProperties;

            if (onCreateRequest != null)
            {
                await onCreateRequest(context, request, requestProperties,
                    context.GetCancellationToken()).ConfigureAwait(false);
            }

            return request;
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            IQueryExecuter queryExecuter)
        {
            QueryRequest request = await CreateQueryRequestInternal(context)
                .ConfigureAwait(false);
            IExecutionResult result = await queryExecuter
                .ExecuteAsync(request, context.GetCancellationToken())
                .ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task WriteResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            if (executionResult is IQueryExecutionResult queryResult)
            {
                string json = queryResult.ToJson();
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                await response.Body.WriteAsync(buffer, 0, buffer.Length)
                    .ConfigureAwait(false);
            }
        }
    }
}
