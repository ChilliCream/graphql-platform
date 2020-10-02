using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Pipeline
{
    public class RemoteRequestMiddleware
    {
        private readonly HttpRequestClient _client = new HttpRequestClient();
        private readonly RequestDelegate _next;
        private readonly IErrorHandler _errorHandler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _schemaName;

        public RemoteRequestMiddleware(
            RequestDelegate next,
            IErrorHandler errorHandler,
            IHttpClientFactory httpClientFactory,
            string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
                    nameof(schemaName));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _errorHandler = errorHandler;
            _httpClientFactory = httpClientFactory;
            _schemaName = schemaName;
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            try
            {
                context.Result = await _client.FetchAsync(
                    context.Request,
                    _httpClientFactory.CreateClient(_schemaName),
                    context.Services.GetServices<IHttpStitchingRequestInterceptor>(),
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
