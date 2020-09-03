using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddleware : MiddlewareBase
    {
        private const string _batchOperations = "batchOperations";
        private readonly IHttpResultSerializer _resultSerializer;
        private readonly IHttpRequestInterceptor _requestInterceptor;
        private readonly IRequestParser _requestParser;

        public HttpPostMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IHttpRequestInterceptor requestInterceptor,
            IRequestParser requestParser,
            NameString schemaName)
            : base(next, executorResolver, schemaName)
        {         
            _resultSerializer = resultSerializer ?? 
                throw new ArgumentNullException(nameof(resultSerializer));
            _requestInterceptor = requestInterceptor ?? 
                throw new ArgumentNullException(nameof(requestInterceptor));
            _requestParser = requestParser ?? 
                throw new ArgumentNullException(nameof(requestParser));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            AllowedContentType contentType = ParseContentType(context.Request.ContentType);

            if (contentType == AllowedContentType.None)
            {
                // the content type is unknown so we will invoke the next middleware.
                await NextAsync(context);
            }
            else
            {
                await HandleRequestAsync(context, contentType);
            }
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            AllowedContentType contentType)
        {
            // first we need to get the request executor to be able to execute requests.
            IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);
            IErrorHandler errorHandler = requestExecutor.Services.GetRequiredService<IErrorHandler>();

            HttpStatusCode? statusCode = null;
            IExecutionResult? result;

            try
            {
                // next we parse the GraphQL request.
                IReadOnlyList<GraphQLRequest> requests = await ReadRequestAsync(
                    contentType, context.Request.Body, context.RequestAborted);

                switch (requests.Count)
                {
                    // if the HTTP request body contains no GraphQL request structure the
                    // whole request is invalid and we will create a GraphQL error response.
                    case 0:
                    {
                        statusCode = HttpStatusCode.BadRequest;
                        IError error = errorHandler.Handle(ErrorHelper.RequestHasNoElements());
                        result = QueryResultBuilder.CreateError(error);
                        break;
                    }
                    // if the HTTP request body contains a single GraphQL request and we do have
                    // the batch operations query parameter specified we need to execute an
                    // operation batch.
                    //
                    // An operation batch consists of a single GraphQL request document that
                    // contains multiple operations. The batch operation query parameter
                    // defines the order in which the operations shall be executed.
                    case 1 when context.Request.Query.ContainsKey(_batchOperations):
                    {
                        string operationNames = context.Request.Query[_batchOperations];

                        if (TryParseOperations(operationNames, out IReadOnlyList<string>? ops))
                        {
                            result = await ExecuteOperationBatchAsync(
                                context, requestExecutor, requests[0], ops);
                        }
                        else
                        {
                            IError error = errorHandler.Handle(ErrorHelper.InvalidRequest());
                            statusCode = HttpStatusCode.BadRequest;
                            result = QueryResultBuilder.CreateError(error);
                        }
                        break;
                    }
                    // if the HTTP request body contains a single GraphQL request and
                    // no batch query parameter is specified we need to execute a single
                    // GraphQL request.
                    //
                    // Most GraphQL requests will be of this type where we want to execute
                    // a single GraphQL query or mutation.
                    case 1:
                    {
                        result = await ExecuteSingleAsync(context, requestExecutor, requests[0]);
                        break;
                    }

                    // if the HTTP request body contains more than one GraphQL request than
                    // we need to execute a request batch where we need to execute multiple
                    // fully specified GraphQL requests at once.
                    default:
                        result = await ExecuteBatchAsync(context, requestExecutor, requests);
                        break;
                }
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
            Debug.Assert(result is not null, "No GraphQL result was created.");
            await WriteResultAsync(context.Response, result, statusCode, context.RequestAborted);
        }

        private async ValueTask WriteResultAsync(
            HttpResponse response,
            IExecutionResult result,
            HttpStatusCode? statusCode,
            CancellationToken cancellationToken)
        {
            response.ContentType = _resultSerializer.GetContentType(result);
            response.StatusCode = (int)(statusCode ?? _resultSerializer.GetStatusCode(result));

            await _resultSerializer.SerializeAsync(result, response.Body, cancellationToken);
        }

        private async Task<IExecutionResult> ExecuteSingleAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            GraphQLRequest request)
        {
            QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);

            await _requestInterceptor.OnCreateAsync(
                context, requestExecutor, requestBuilder, context.RequestAborted);

            return await requestExecutor.ExecuteAsync(
                requestBuilder.Create(), context.RequestAborted);
        }

        private async Task<IBatchQueryResult> ExecuteOperationBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            var requestBatch = new IReadOnlyQueryRequest[operationNames.Count];

            for (var i = 0; i < operationNames.Count; i++)
            {
                QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);
                requestBuilder.SetOperation(operationNames[i]);

                await _requestInterceptor.OnCreateAsync(
                    context, requestExecutor, requestBuilder, context.RequestAborted);

                requestBatch[i] = requestBuilder.Create();
            }

            return await requestExecutor.ExecuteBatchAsync(
                requestBatch, cancellationToken: context.RequestAborted);
        }

        private async Task<IBatchQueryResult> ExecuteBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IReadOnlyList<GraphQLRequest> requests)
        {
            var requestBatch = new IReadOnlyQueryRequest[requests.Count];

            for (var i = 0; i < requests.Count; i++)
            {
                QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(requests[0]);

                await _requestInterceptor.OnCreateAsync(
                    context, requestExecutor, requestBuilder, context.RequestAborted);

                requestBatch[i] = requestBuilder.Create();
            }

            return await requestExecutor.ExecuteBatchAsync(
                requestBatch, cancellationToken: context.RequestAborted);
        }

        private async Task<IReadOnlyList<GraphQLRequest>> ReadRequestAsync(
            AllowedContentType contentType,
            Stream body,
            CancellationToken cancellationToken)
        {
            if (contentType == AllowedContentType.Json)
            {
                return await _requestParser.ReadJsonRequestAsync(body, cancellationToken);
            }
            throw new NotSupportedException();
        }

        private static AllowedContentType ParseContentType(string s)
        {
            ReadOnlySpan<char> span = s.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == ';')
                {
                    span = span.Slice(0, i);
                    break;
                }
            }

            if (span.SequenceEqual(ContentType.JsonSpan()))
            {
                return AllowedContentType.Json;
            }

            return AllowedContentType.None;
        }

        private static bool TryParseOperations(
            string operationNameString,
            [NotNullWhen(true)] out IReadOnlyList<string>? operationNames)
        {
            var reader = new Utf8GraphQLReader(Encoding.UTF8.GetBytes(operationNameString));
            reader.Read();

            if (reader.Kind != TokenKind.LeftBracket)
            {
                operationNames = null;
                return false;
            }

            var names = new List<string>();

            while (reader.Read() && reader.Kind == TokenKind.Name)
            {
                names.Add(reader.GetName());
            }

            if (reader.Kind != TokenKind.RightBracket)
            {
                operationNames = null;
                return false;
            }

            operationNames = names;
            return true;
        }        
    }
}
