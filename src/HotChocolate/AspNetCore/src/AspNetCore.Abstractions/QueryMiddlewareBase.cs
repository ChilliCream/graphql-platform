using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Server;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// A base <c>GraphQL</c> query middleware.
    /// </summary>
    public abstract class QueryMiddlewareBase
    {
        private const int _badRequest = 400;
        private const int _ok = 200;

        private readonly Func<HttpContext, bool> _isPathValid;
        private readonly IQueryResultSerializer _serializer;

        private IQueryRequestInterceptor<HttpContext> _interceptor;
        private bool _interceptorInitialized;

        protected QueryMiddlewareBase(
            RequestDelegate next,
            IPathOptionAccessor options,
            IQueryResultSerializer serializer,
            IErrorHandler errorHandler)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _serializer = serializer
                ?? throw new ArgumentNullException(nameof(serializer));
            ErrorHandler = errorHandler
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

        protected IErrorHandler ErrorHandler { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (_isPathValid(context) && CanHandleRequest(context))
            {
                try
                {
                    await HandleRequestAsync(context)
                        .ConfigureAwait(false);
                }
                catch (ArgumentException)
                {
                    context.Response.StatusCode = _badRequest;
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
                        .SetCode(ErrorCodes.Execution.SyntaxError)
                        .Build();
                    ErrorHandler.Handle(error);

                    var errorResult = QueryResult.CreateError(error);

                    SetResponseHeaders(context.Response, _serializer.ContentType);
                    await _serializer.SerializeAsync(errorResult, context.Response.Body)
                        .ConfigureAwait(false);
                }
                catch (QueryException ex)
                {
                    var errorResult = QueryResult.CreateError(
                        ErrorHandler.Handle(ex.Errors));
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

        private async Task HandleRequestAsync(
                  HttpContext context)
        {
            await ExecuteRequestAsync(context, context.RequestServices)
                .ConfigureAwait(false);
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
