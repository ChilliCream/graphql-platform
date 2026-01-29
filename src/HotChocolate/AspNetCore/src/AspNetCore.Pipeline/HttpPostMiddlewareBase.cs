using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Execution.RequestFlags;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public abstract class HttpPostMiddlewareBase : MiddlewareBase
{
    private const string BatchOperations = "batchOperations";

    protected HttpPostMiddlewareBase(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor)
        : base(next, executor)
    {
    }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.ParseContentType() is RequestContentType.Json)
        {
            var ct = context.RequestAborted;
            var session = await Executor.GetOrCreateSessionAsync(ct);

            using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpPost))
            {
                await HandleRequestAsync(context, session, ct);
            }
        }
        else
        {
            // if the request is not a post request, we will just invoke the next
            // middleware and do nothing:
            await NextAsync(context);
        }
    }

    protected async Task HandleRequestAsync(HttpContext context, ExecutorSession session, CancellationToken ct)
    {
        HttpStatusCode? statusCode = null;
        IExecutionResult? result;

        // next we will inspect the accept headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);
        var acceptMediaTypes = headerResult.AcceptMediaTypes;

        // if we cannot parse all media types that we provided we will fail the request
        // with a 400 Bad Request.
        if (headerResult.HasError)
        {
            // in this case accept headers were specified, and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.BadRequest;

            var errors = headerResult.ErrorResult.Errors;
            result = headerResult.ErrorResult;
            session.DiagnosticEvents.HttpRequestError(context, errors[0]);
            goto HANDLE_RESULT;
        }

        var requestFlags = session.CreateRequestFlags(headerResult.AcceptMediaTypes);

        // if the request defines accept header values of which we cannot handle any provided
        // media type then we will fail the request with 406 Not Acceptable.
        if (requestFlags is None)
        {
            // in this case accept headers were specified, and we will
            // respond with proper error codes
            acceptMediaTypes = HeaderUtilities.GraphQLResponseContentTypes;
            statusCode = HttpStatusCode.NotAcceptable;

            var error = ErrorHelper.NoSupportedAcceptMediaType();
            result = OperationResult.FromError(error);
            session.DiagnosticEvents.HttpRequestError(context, error);
            goto HANDLE_RESULT;
        }

        // next we parse the GraphQL request.
        GraphQLRequest[] requests;

        using (session.DiagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                requests = await ParseRequestsFromBodyAsync(context, session);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                statusCode = HttpStatusCode.BadRequest;
                var errors = session.Handle(ex.Errors);
                result = OperationResult.FromError([.. errors]);
                session.DiagnosticEvents.ParserErrors(context, errors);
                goto HANDLE_RESULT;
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                var error = ErrorBuilder.FromException(ex).Build();
                result = OperationResult.FromError(error);
                session.DiagnosticEvents.HttpRequestError(context, error);
                goto HANDLE_RESULT;
            }
        }

        // after successfully parsing the request we now will attempt to execute the request.
        try
        {
            switch (requests.Length)
            {
                // if the HTTP request body contains no GraphQL request structure the
                // whole request is invalid, and we will create a GraphQL error response.
                case 0:
                {
                    statusCode = HttpStatusCode.BadRequest;
                    var error = session.Handle(ErrorHelper.RequestHasNoElements());
                    result = OperationResult.FromError(error);
                    session.DiagnosticEvents.HttpRequestError(context, error);
                    break;
                }

                // if the HTTP request body contains a single GraphQL request, and we do have
                // the batch operations query parameter specified we need to execute an
                // operation batch.
                //
                // An operation batch consists of a single GraphQL request document that
                // contains multiple operations. The batch operation query parameter
                // defines the order in which the operations shall be executed.
                case 1 when context.Request.Query.ContainsKey(BatchOperations):
                {
                    string? operationNames = context.Request.Query[BatchOperations];

                    if (!string.IsNullOrEmpty(operationNames)
                        && TryParseOperations(operationNames, out var ops)
                        && GetOptions(context).EnableBatching)
                    {
                        result = await session.ExecuteOperationBatchAsync(context, requests[0], requestFlags, ops);
                    }
                    else
                    {
                        var error = session.Handle(ErrorHelper.InvalidRequest());
                        statusCode = HttpStatusCode.BadRequest;
                        result = OperationResult.FromError(error);
                        session.DiagnosticEvents.HttpRequestError(context, error);
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
                    result = await session.ExecuteSingleAsync(context, requests[0], requestFlags);
                    break;

                // if the HTTP request body contains more than one GraphQL request than
                // we need to execute a request batch where we need to execute multiple
                // fully specified GraphQL requests at once.
                default:
                    if (GetOptions(context).EnableBatching)
                    {
                        result = await session.ExecuteBatchAsync(context, requests, requestFlags);
                    }
                    else
                    {
                        var error = session.Handle(ErrorHelper.InvalidRequest());
                        statusCode = HttpStatusCode.BadRequest;
                        result = OperationResult.FromError(error);
                        session.DiagnosticEvents.HttpRequestError(context, error);
                    }
                    break;
            }
        }
        catch (GraphQLException ex)
        {
            // This allows extensions to throw GraphQL exceptions in the GraphQL interceptor.
            statusCode = null; // we let the serializer determine the status code.
            result = OperationResult.FromError([.. ex.Errors]);

            foreach (var error in ex.Errors)
            {
                session.DiagnosticEvents.HttpRequestError(context, error);
            }
        }
        catch (Exception ex)
        {
            statusCode = HttpStatusCode.InternalServerError;
            var error = ErrorBuilder.FromException(ex).Build();
            result = OperationResult.FromError(error);
            session.DiagnosticEvents.HttpRequestError(context, error);
        }

HANDLE_RESULT:
        IDisposable? formatScope = null;

        try
        {
            // if cancellation is requested we will not try to attempt to write the result to the
            // response stream.
            if (ct.IsCancellationRequested)
            {
                return;
            }

            // in any case we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null, "No GraphQL result was created.");

            if (result is OperationResult operationResult)
            {
                formatScope = session.DiagnosticEvents.FormatHttpResponse(context, operationResult);
            }

            await session.WriteResultAsync(context, result, acceptMediaTypes, statusCode);
        }
        finally
        {
            // we must dispose the diagnostic scope first.
            formatScope?.Dispose();

            // query results use pooled memory and need to be disposed after we have
            // used them.
            if (result is not null)
            {
                await result.DisposeAsync();
            }
        }
    }

    protected virtual async ValueTask<GraphQLRequest[]> ParseRequestsFromBodyAsync(
        HttpContext context,
        ExecutorSession session)
    {
        var requests =
            await session.RequestParser.ParseRequestAsync(
                context.Request.BodyReader,
                context.RequestAborted);

        foreach (var request in requests)
        {
            context.Response.RegisterForDispose(request);
        }

        return requests;
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
