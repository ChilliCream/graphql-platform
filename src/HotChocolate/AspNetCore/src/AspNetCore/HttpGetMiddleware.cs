using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpGetMiddleware : MiddlewareBase
    {
        private readonly IHttpRequestParser _requestParser;

        public HttpGetMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IHttpRequestParser requestParser,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            _requestParser = requestParser ??
                throw new ArgumentNullException(nameof(requestParser));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                await HandleRequestAsync(context);
            }
            else
            {
                // if the request is not a get request or if the content type is not correct
                // we will just invoke the next middleware and do nothing.
                await NextAsync(context);
            }
        }

        private async Task HandleRequestAsync(HttpContext context)
        {
            // first we need to get the request executor to be able to execute requests.
            IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);
            IHttpRequestInterceptor requestInterceptor = requestExecutor.GetRequestInterceptor();
            IErrorHandler errorHandler = requestExecutor.GetErrorHandler();

            HttpStatusCode? statusCode = null;
            IExecutionResult? result;

            try
            {
                // next we parse the GraphQL request.
                GraphQLRequest request = _requestParser.ReadParamsRequest(context.Request.Query);
                result = await ExecuteSingleAsync(
                    context, requestExecutor, requestInterceptor, request);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                statusCode = HttpStatusCode.BadRequest;
                result = QueryResultBuilder.CreateError(errorHandler.Handle(ex.Errors));
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                IError error = errorHandler.CreateUnexpectedError(ex).Build();
                result = QueryResultBuilder.CreateError(error);
            }

            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null!, "No GraphQL result was created.");
            await WriteResultAsync(context.Response, result, statusCode, context.RequestAborted);
        }
    }
}
