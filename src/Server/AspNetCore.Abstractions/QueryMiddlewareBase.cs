using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Server;
using HotChocolate.Language;

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

        private readonly Func<HttpContext, bool> _isPathValid;
        private readonly IQueryResultSerializer _serializer;

        private IQueryRequestInterceptor<HttpContext> _interceptor;
        private bool _interceptorInitialized;

#if ASPNETCLASSIC
        private readonly IServiceProvider _services;
        private readonly OwinContextAccessor _accessor;

        protected QueryMiddlewareBase(
            RequestDelegate next,
            IPathOptionAccessor options,
            OwinContextAccessor owinContextAccessor,
            IServiceProvider services,
            IQueryResultSerializer serializer)
            : base(next)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _accessor = owinContextAccessor;
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));

            if (options.Path.Value.Length > 1)
            {
                var path1 = new PathString(options.Path.Value.TrimEnd('/'));
                PathString path2 = path1.Add(new PathString("/"));
                _isPathValid = ctx => ctx.IsValidPath(path1, path2);
            }
            else
            {
                _isPathValid = ctx => ctx.IsValidPath(options.Path);
            }
        }
#else
        protected QueryMiddlewareBase(
            RequestDelegate next,
            IPathOptionAccessor options,
            IQueryResultSerializer serializer)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));

            Next = next;

            if (options.Path.Value.Length > 1)
            {
                var path1 = new PathString(options.Path.Value.TrimEnd('/'));
                PathString path2 = path1.Add(new PathString("/"));
                _isPathValid = ctx => ctx.IsValidPath(path1, path2);
            }
            else
            {
                _isPathValid = ctx => ctx.IsValidPath(options.Path);
            }
        }

        protected RequestDelegate Next { get; }
#endif

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
                catch (SyntaxException ex)
                {
                    IError error = ErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .AddLocation(ex.Line, ex.Column)
                        .Build();

                    var errorResult = QueryResult.CreateError(error);

                    SetResponseHeaders(context.Response, _serializer.ContentType);
                    await _serializer.SerializeAsync(errorResult, context.Response.Body)
                        .ConfigureAwait(false);
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
        private async Task HandleRequestAsync(
            HttpContext context)
        {
            if (_accessor != null)
            {
                _accessor.OwinContext = context;
            }

            using (IServiceScope serviceScope = _services.CreateScope())
            {
                IServiceProvider services =
                    context.CreateRequestServices(
                        serviceScope.ServiceProvider);

                await ExecuteRequestAsync(context, services)
                    .ConfigureAwait(false);
            }
        }
#else
        private async Task HandleRequestAsync(
                  HttpContext context)
        {
            await ExecuteRequestAsync(context, context.RequestServices)
                .ConfigureAwait(false);
        }
#endif

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
                _interceptor = services
                    .GetService<IQueryRequestInterceptor<HttpContext>>();
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

        protected static void SetResponseHeaders(
            HttpResponse response,
            string contentType)
        {
            response.ContentType = contentType ?? ContentType.Json;
            response.StatusCode = _ok;
        }
    }
}
