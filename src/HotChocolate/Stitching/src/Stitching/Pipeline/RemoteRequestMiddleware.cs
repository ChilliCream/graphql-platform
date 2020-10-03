using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Pipeline
{
    internal class RemoteRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IErrorHandler _errorHandler;
        private readonly HttpRequestClient _httpRequestClient;

        public RemoteRequestMiddleware(
            RequestDelegate next,
            IErrorHandler errorHandler,
            HttpRequestClient httpRequestClient)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _errorHandler = errorHandler;
            _httpRequestClient = httpRequestClient;
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            try
            {
                context.Result =
                    await _httpRequestClient.FetchAsync(
                        context.Request,
                        context.Schema.Name,
                        context.RequestAborted)
                        .ConfigureAwait(false);
            }
            catch(HttpRequestException ex)
            {
                IError error = _errorHandler.CreateUnexpectedError(ex)
                    .SetCode(ErrorCodes.HttpRequestException)
                    .Build();

                context.Exception = ex;
                context.Result = QueryResultBuilder.CreateError(error);
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
