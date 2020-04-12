using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;
using static HotChocolate.Execution.QueryResultBuilder;
using System.Threading;

namespace HotChocolate.AspNetCore
{
    public abstract class QueryMiddlewareBase
    {
        protected const int BadRequest = 400;
        protected const int OK = 200;

        private readonly Func<HttpContext, bool> _isPathValid;
        private readonly IQueryResultSerializer _serializer;

        private IQueryRequestInterceptor<HttpContext>? _interceptor;
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

        public async Task InvokeAsync(HttpContext context)
        {
            if (_isPathValid(context) && CanHandleRequest(context))
            {
                var httpHelper = new HttpHelper(context, _serializer);

                try
                {
                    await HandleRequestAsync(httpHelper).ConfigureAwait(false);
                }
                catch (SyntaxException ex)
                {
                    IError error = ErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .AddLocation(ex.Line, ex.Column)
                        .SetCode(ErrorCodes.Execution.SyntaxError)
                        .Build();

                    httpHelper.StatusCode = OK;
                    httpHelper.Result = CreateError(ErrorHandler.Handle(error));
                }
                catch (QueryException ex)
                {
                    httpHelper.StatusCode = OK;
                    httpHelper.Result = CreateError(ErrorHandler.Handle(ex.Errors));
                }
                catch (Exception ex)
                {
                    IError error = ErrorHandler.Handle(
                        ErrorBuilder.New()
                            .SetMessage(ex.Message)
                            .SetException(ex)
                            .Build());

                    httpHelper.StatusCode = BadRequest;
                    httpHelper.Result = CreateError(error);
                }

                await httpHelper.WriteAsync().ConfigureAwait(false);
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

        private async Task HandleRequestAsync(HttpHelper httpHelper)
        {
            await ExecuteRequestAsync(httpHelper).ConfigureAwait(false);
        }

        protected abstract Task ExecuteRequestAsync(HttpHelper httpHelper);

        protected async Task<IReadOnlyQueryRequest> BuildRequestAsync(
            HttpContext context,
            IServiceProvider services,
            IQueryRequestBuilder builder)
        {
            if (!_interceptorInitialized)
            {
                _interceptor = services.GetService<IQueryRequestInterceptor<HttpContext>>();
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

            builder.TrySetServices(services);
            builder.TryAddProperty(nameof(HttpContext), context);
            builder.TryAddProperty(nameof(ClaimsPrincipal), context.GetUser());

            if (context.IsTracingEnabled())
            {
                builder.TryAddProperty(ContextDataKeys.EnableTracing, true);
            }

            return builder.Create();
        }
    }

    public class HttpHelper
    {
        public HttpHelper(HttpContext context, IQueryResultSerializer serializer)
        {
            Context = context;
            Serializer = serializer;
        }

        public HttpContext Context { get; }

        public IServiceProvider Services => Context.RequestServices;

        public IQueryResultSerializer Serializer { get; }

        public IResponseStreamSerializer? StreamSerializer { get; set; }

        public int StatusCode { get; set; } = 200;

        public IExecutionResult? Result { get; set; }

        public async Task WriteAsync()
        {
            if (Result is IReadOnlyQueryResult result)
            {
                SetResponseHeaders(Serializer.ContentType, StatusCode);
                await Serializer.SerializeAsync(
                    result, Context.Response.Body, Context.RequestAborted);
            }
            else if (Result is IResponseStream stream && StreamSerializer is { })
            {
                SetResponseHeaders(StreamSerializer.ContentType, StatusCode);
                await StreamSerializer.SerializeAsync(
                    stream, Context.Response.Body, Context.RequestAborted);
            }
            else
            {
                SetResponseHeaders(Serializer.ContentType, StatusCode);
                await Serializer.SerializeAsync(
                    CreateError(ErrorBuilder.New().SetMessage("Unexpected Error").Build()),
                    Context.Response.Body,
                    Context.RequestAborted);
            }
        }

        private void SetResponseHeaders(string contentType, int statusCode)
        {
            Context.Response.ContentType = contentType;
            Context.Response.StatusCode = statusCode;
        }
    }
}
