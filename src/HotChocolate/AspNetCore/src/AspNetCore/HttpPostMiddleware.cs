using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddleware : MiddlewareBase
    {
        private const string _batchOperations = "batchOperations";
        protected IHttpRequestParser RequestParser { get; }

        public HttpPostMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IHttpRequestParser requestParser,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
        {
            RequestParser = requestParser ??
                throw new ArgumentNullException(nameof(requestParser));
        }

        public virtual async Task InvokeAsync(HttpContext context)
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                ParseContentType(context) == AllowedContentType.Json)
            {
                await HandleRequestAsync(context, AllowedContentType.Json);
            }
            else
            {
                // if the request is not a post request we will just invoke the next
                // middleware and do nothing:
                await NextAsync(context);
            }
        }

        protected virtual ValueTask<IReadOnlyList<GraphQLRequest>> GetRequestsFromBody(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            return RequestParser.ReadJsonRequestAsync(request.Body, cancellationToken);
        }

        protected async Task HandleRequestAsync(
            HttpContext context,
            AllowedContentType contentType)
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
                IReadOnlyList<GraphQLRequest> requests =
                    await GetRequestsFromBody(context.Request, context.RequestAborted);

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
                                context, requestExecutor, requestInterceptor, requests[0], ops);
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
                        result = await ExecuteSingleAsync(
                            context, requestExecutor, requestInterceptor, requests[0]);
                        break;
                    }

                    // if the HTTP request body contains more than one GraphQL request than
                    // we need to execute a request batch where we need to execute multiple
                    // fully specified GraphQL requests at once.
                    default:
                        result = await ExecuteBatchAsync(
                            context, requestExecutor, requestInterceptor, requests);
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
