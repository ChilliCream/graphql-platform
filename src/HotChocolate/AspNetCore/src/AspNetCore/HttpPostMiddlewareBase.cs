using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Execution.GraphQLRequestFlags;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public class HttpPostMiddlewareBase : MiddlewareBase
{
    private const string _batchOperations = "batchOperations";

    protected HttpPostMiddlewareBase(
        HttpRequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResponseFormatter responseFormatter,
        IHttpRequestParser requestParser,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName)
        : base(next, executorResolver, responseFormatter, schemaName)
    {
        RequestParser = requestParser ??
            throw new ArgumentNullException(nameof(requestParser));
        DiagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    protected IHttpRequestParser RequestParser { get; }

    protected IServerDiagnosticEvents DiagnosticEvents { get; }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            ParseContentType(context) is RequestContentType.Json)
        {
            if (!IsDefaultSchema)
            {
                context.Items[WellKnownContextData.SchemaName] = SchemaName;
            }

            using (DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpPost))
            {
                await HandleRequestAsync(context);
            }
        }
        else
        {
            // if the request is not a post request we will just invoke the next
            // middleware and do nothing:
            await NextAsync(context);
        }
    }

    protected async Task HandleRequestAsync(HttpContext context)
    {
        HttpStatusCode? statusCode = null;
        IExecutionResult? result;

        // first we need to get the request executor to be able to execute requests.
        var requestExecutor = await GetExecutorAsync(context.RequestAborted);
        var requestInterceptor = requestExecutor.GetRequestInterceptor();
        var errorHandler = requestExecutor.GetErrorHandler();
        context.Items[WellKnownContextData.RequestExecutor] = requestExecutor;

        // next we will inspect the accept headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);
        var acceptMediaTypes = headerResult.AcceptMediaTypes;

        // if we cannot parse all media types that we provided we will fail the request
        // with a 400 Bad Request.
        if (headerResult.HasError)
        {
            // in this case accept headers were specified and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.BadRequest;

            var errors = headerResult.ErrorResult.Errors!;
            result = headerResult.ErrorResult;
            DiagnosticEvents.HttpRequestError(context, errors[0]);
            goto HANDLE_RESULT;
        }

        var requestFlags = CreateRequestFlags(headerResult.AcceptMediaTypes);

        // if the request defines accept header values of which we cannot handle any provided
        // media type then we will fail the request with 406 Not Acceptable.
        if (requestFlags is None)
        {
            // in this case accept headers were specified and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.NotAcceptable;

            var error = ErrorHelper.NoSupportedAcceptMediaType();
            result = OperationResultBuilder.CreateError(error);
            DiagnosticEvents.HttpRequestError(context, error);
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        IReadOnlyList<GraphQLRequest> requests;

        using (DiagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                requests = await ParseRequestsFromBodyAsync(context.Request, context.RequestAborted);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                statusCode = HttpStatusCode.BadRequest;
                var errors = errorHandler.Handle(ex.Errors);
                result = OperationResultBuilder.CreateError(errors);
                DiagnosticEvents.ParserErrors(context, errors);
                goto HANDLE_RESULT;
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                result = OperationResultBuilder.CreateError(error);
                DiagnosticEvents.HttpRequestError(context, error);
                goto HANDLE_RESULT;
            }
        }

        // after successfully parsing the request we now will attempt to execute the request.
        try
        {
            switch (requests.Count)
            {
                // if the HTTP request body contains no GraphQL request structure the
                // whole request is invalid and we will create a GraphQL error response.
                case 0:
                {
                    statusCode = HttpStatusCode.BadRequest;
                    var error = errorHandler.Handle(ErrorHelper.RequestHasNoElements());
                    result = OperationResultBuilder.CreateError(error);
                    DiagnosticEvents.HttpRequestError(context, error);
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
                    string? operationNames = context.Request.Query[_batchOperations];

                    if (!string.IsNullOrEmpty(operationNames) &&
                        TryParseOperations(operationNames, out var ops) &&
                        GetOptions(context).EnableBatching)
                    {
                        result = await ExecuteOperationBatchAsync(
                            context,
                            requestExecutor,
                            requestInterceptor,
                            DiagnosticEvents,
                            requests[0],
                            requestFlags,
                            ops);
                    }
                    else
                    {
                        var error = errorHandler.Handle(ErrorHelper.InvalidRequest());
                        statusCode = HttpStatusCode.BadRequest;
                        result = OperationResultBuilder.CreateError(error);
                        DiagnosticEvents.HttpRequestError(context, error);
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
                        context,
                        requestExecutor,
                        requestInterceptor,
                        DiagnosticEvents,
                        requests[0],
                        requestFlags);
                    break;
                }

                // if the HTTP request body contains more than one GraphQL request than
                // we need to execute a request batch where we need to execute multiple
                // fully specified GraphQL requests at once.
                default:
                    if (GetOptions(context).EnableBatching)
                    {
                        result = await ExecuteBatchAsync(
                            context,
                            requestExecutor,
                            requestInterceptor,
                            DiagnosticEvents,
                            requests,
                            requestFlags);
                    }
                    else
                    {
                        var error = errorHandler.Handle(ErrorHelper.InvalidRequest());
                        statusCode = HttpStatusCode.BadRequest;
                        result = OperationResultBuilder.CreateError(error);
                        DiagnosticEvents.HttpRequestError(context, error);
                    }
                    break;
            }
        }
        catch (GraphQLException ex)
        {
            // This allows extensions to throw GraphQL exceptions in the GraphQL interceptor.
            statusCode = null; // we let the serializer determine the status code.
            result = OperationResultBuilder.CreateError(ex.Errors);

            foreach (var error in ex.Errors)
            {
                DiagnosticEvents.HttpRequestError(context, error);
            }
        }
        catch (Exception ex)
        {
            statusCode = HttpStatusCode.InternalServerError;
            var error = errorHandler.CreateUnexpectedError(ex).Build();
            result = OperationResultBuilder.CreateError(error);
            DiagnosticEvents.HttpRequestError(context, error);
        }

        HANDLE_RESULT:
        IDisposable? formatScope = null;

        try
        {
            // if cancellation is requested we will not try to attempt to write the result to the
            // response stream.
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null, "No GraphQL result was created.");

            if (result is IOperationResult queryResult)
            {
                formatScope = DiagnosticEvents.FormatHttpResponse(context, queryResult);
            }

            await WriteResultAsync(context, result, acceptMediaTypes, statusCode);
        }
        finally
        {
            // we must dispose the diagnostic scope first.
            formatScope?.Dispose();

            // query results use pooled memory an need to be disposed after we have
            // used them.
            if (result is not null)
            {
                await result.DisposeAsync();
            }
        }
    }

    protected virtual ValueTask<IReadOnlyList<GraphQLRequest>> ParseRequestsFromBodyAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
        => RequestParser.ParseRequestAsync(request.Body, cancellationToken);

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
