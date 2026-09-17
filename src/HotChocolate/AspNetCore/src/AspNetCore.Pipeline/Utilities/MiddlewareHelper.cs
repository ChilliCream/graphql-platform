using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Utilities;

internal static class MiddlewareHelper
{
    public static ValidateAcceptContentTypeResult ValidateAcceptContentType(
        HttpContext context,
        ExecutorSession executorSession)
    {
        // first, we will inspect the accept-headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);

        // if we cannot parse all media types that the user provided, we will fail the request
        // with a 400 Bad Request.
        if (headerResult.HasError)
        {
            var errors = headerResult.ErrorResult.Errors;
            executorSession.DiagnosticEvents.HttpRequestError(context, errors[0]);

            return new ValidateAcceptContentTypeResult(
                headerResult.ErrorResult,
                HttpStatusCode.BadRequest);
        }

        var requestFlags = executorSession.CreateRequestFlags(headerResult.AcceptMediaTypes);

        // if the request defines accept header values of which we cannot handle any provided
        // media type, then we will fail the request with 406 Not Acceptable.
        if (requestFlags is RequestFlags.None)
        {
            var error = ErrorHelper.NoSupportedAcceptMediaType();
            executorSession.DiagnosticEvents.HttpRequestError(context, error);

            // The client's own media types travel with the error so the formatter can tell
            // whether the response body would be readable. It writes the error in the server's
            // default format while that format is still acceptable, and sends the status alone
            // when the client has ruled out everything the server can produce.
            return new ValidateAcceptContentTypeResult(
                error,
                HttpStatusCode.NotAcceptable,
                headerResult.AcceptMediaTypes);
        }

        return new ValidateAcceptContentTypeResult(
            requestFlags,
            headerResult.AcceptMediaTypes);
    }

    public static ParseRequestResult ParseRequestFromParams(
        HttpContext context,
        ExecutorSession executorSession)
    {
        using (executorSession.DiagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                var request = executorSession.ParseRequestFromParams(context.Request.Query);
                context.Response.RegisterForDispose(request);
                return new ParseRequestResult(request);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the request parameters couldn't be
                // parsed. In this case, we will return HTTP status code 400 and return a
                // GraphQL error result. A document syntax error leaves the status unset so the
                // formatter applies the per-content-type rule.
                var errors = executorSession.Handle(ex.Errors);
                executorSession.DiagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(
                    CreateRequestErrorResult(ex, errors),
                    IsDocumentSyntaxError(ex.Errors) ? null : HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = ErrorBuilder.FromException(ex).Build();
                executorSession.DiagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }
    }

    public static ParseRequestResult ParseVariablesAndExtensionsFromParams(
        string operationId,
        string? operationName,
        HttpContext context,
        ExecutorSession executorSession)
    {
        using (executorSession.DiagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                var request =
                    executorSession.ParsePersistedOperationRequestFromParams(
                        operationId,
                        operationName,
                        context.Request.Query);
                context.Response.RegisterForDispose(request);
                return new ParseRequestResult(request);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case, we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = executorSession.Handle(ex.Errors);
                executorSession.DiagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(
                    CreateRequestErrorResult(ex, errors),
                    HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = ErrorBuilder.FromException(ex).Build();
                executorSession.DiagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }
    }

    public static async Task<ParseRequestResult> ParseSingleRequestFromBodyAsync(
        string operationId,
        string? operationName,
        HttpContext context,
        ExecutorSession executorSession)
    {
        GraphQLRequest request;
        using (executorSession.DiagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                request =
                    await executorSession.ParsePersistedOperationRequestAsync(
                        operationId,
                        operationName,
                        context.Request.BodyReader,
                        context.RequestAborted);
                context.Response.RegisterForDispose(request);
            }
            catch (InvalidGraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case, we will return HTTP status code 400 and return a
                // GraphQL error result.
                IError error = new Error { Message = ex.Message };
                var handledError = executorSession.Handle(error);
                executorSession.DiagnosticEvents.ParserErrors(context, [handledError]);
                return new ParseRequestResult(
                    CreateRequestErrorResult(ex, handledError),
                    HttpStatusCode.BadRequest);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case, we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = executorSession.Handle(ex.Errors);
                executorSession.DiagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(
                    CreateRequestErrorResult(ex, errors),
                    HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = ErrorBuilder.FromException(ex).Build();
                executorSession.DiagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }

        return new ParseRequestResult(request);
    }

    /// <summary>
    /// Creates the result for a request the parser rejected. The result of a document the
    /// parser could not read carries a <c>400</c>, and the result of a request the parser read
    /// but could not accept as a GraphQL over HTTP request is marked as not well-formed. The
    /// exception the parser threw decides the kind, since an error filter may have rewritten
    /// the errors that are written to the response.
    /// </summary>
    public static OperationResult CreateRequestErrorResult(
        GraphQLRequestException exception,
        IReadOnlyList<IError> handledErrors)
    {
        var result = OperationResult.FromError([.. handledErrors]);

        if (IsDocumentSyntaxError(exception.Errors))
        {
            result.ContextData = result.ContextData.Add(
                ExecutionContextData.HttpStatusCode,
                HttpStatusCode.BadRequest);
        }
        else if (IsRequestNotWellFormed(exception))
        {
            result.ContextData = result.ContextData.Add(
                HttpResultContextData.RequestNotWellFormed,
                null);
        }

        return result;
    }

    /// <summary>
    /// Creates the result for a request whose structure the parser rejected. The result is
    /// marked as not well-formed unless the body was not JSON.
    /// </summary>
    public static OperationResult CreateRequestErrorResult(
        InvalidGraphQLRequestException exception,
        IError handledError)
    {
        var result = OperationResult.FromError(handledError);

        if (IsRequestNotWellFormed(exception))
        {
            result.ContextData = result.ContextData.Add(
                HttpResultContextData.RequestNotWellFormed,
                null);
        }

        return result;
    }

    /// <summary>
    /// Whether every error describes a GraphQL document the parser could not read.
    /// </summary>
    public static bool IsDocumentSyntaxError(IReadOnlyList<IError> errors)
    {
        if (errors.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < errors.Count; i++)
        {
            if (!string.Equals(
                errors[i].Code,
                ErrorCodes.Server.SyntaxError,
                StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether the exception describes a request the parser read but could not accept as a
    /// GraphQL over HTTP request: a body that is not a request object, a parameter of the
    /// wrong type, a request parameter that is not valid JSON, or a request that names
    /// neither a document nor a document ID. A body that is not JSON is not such a request.
    /// </summary>
    private static bool IsRequestNotWellFormed(GraphQLRequestException exception)
    {
        if (exception.InnerException is InvalidGraphQLRequestException cause)
        {
            return IsRequestNotWellFormed(cause);
        }

        // a bare JsonException is a request parameter that is not valid JSON. the body parsers
        // wrap the JsonException of a body that is not JSON in an InvalidGraphQLRequestException.
        if (exception.InnerException is JsonException)
        {
            return true;
        }

        var errors = exception.Errors;

        if (errors.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < errors.Count; i++)
        {
            if (!string.Equals(
                errors[i].Code,
                ErrorCodes.Server.QueryAndIdMissing,
                StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRequestNotWellFormed(InvalidGraphQLRequestException exception)
        => exception.InnerException is not JsonException;

    public static RequestFlags DetermineHttpGetRequestFlags(
        RequestFlags requestFlags,
        GraphQLServerOptions options)
    {
        if (options is null or { AllowedGetOperations: AllowedGetOperations.Query })
        {
            requestFlags = (requestFlags & RequestFlags.AllowStreams) == RequestFlags.AllowStreams
                ? RequestFlags.AllowQuery | RequestFlags.AllowStreams
                : RequestFlags.AllowQuery;
        }
        else
        {
            var flags = options.AllowedGetOperations;
            var newRequestFlags = RequestFlags.None;

            if ((flags & AllowedGetOperations.Query) == AllowedGetOperations.Query)
            {
                newRequestFlags |= RequestFlags.AllowQuery;
            }

            if ((flags & AllowedGetOperations.Mutation) == AllowedGetOperations.Mutation)
            {
                newRequestFlags |= RequestFlags.AllowMutation;
            }

            if ((flags & AllowedGetOperations.Subscription) == AllowedGetOperations.Subscription
                && (requestFlags & RequestFlags.AllowSubscription) == RequestFlags.AllowSubscription)
            {
                newRequestFlags |= RequestFlags.AllowSubscription;
            }

            if ((requestFlags & RequestFlags.AllowStreams) == RequestFlags.AllowStreams)
            {
                newRequestFlags |= RequestFlags.AllowStreams;
            }

            requestFlags = newRequestFlags;
        }

        return requestFlags;
    }

    public static async Task<ExecuteRequestResult> ExecuteRequestAsync(
        GraphQLRequest request,
        RequestFlags flags,
        HttpContext context,
        ExecutorSession executorSession)
    {
        // after successfully parsing the request, we now will attempt to execute the request.

        try
        {
            executorSession.DiagnosticEvents.StartSingleRequest(context, request);

            var requestBuilder = OperationRequestBuilder.From(request);
            requestBuilder.SetFlags(flags);

            await executorSession.OnCreateAsync(
                context,
                requestBuilder,
                context.RequestAborted);

            var result = await executorSession.ExecuteAsync(
                requestBuilder.Build(),
                context.RequestAborted);

            return new ExecuteRequestResult(result);
        }
        catch (GraphQLException ex)
        {
            // this allows extensions to throw GraphQL exceptions in the GraphQL interceptor.
            // we let the serializer determine the status code.
            foreach (var error in ex.Errors)
            {
                executorSession.DiagnosticEvents.HttpRequestError(context, error);
            }

            return new ExecuteRequestResult(
                OperationResult.FromError([.. ex.Errors]));
        }
        catch (Exception ex)
        {
            var error = ErrorBuilder.FromException(ex).Build();
            executorSession.DiagnosticEvents.HttpRequestError(context, error);
            return new ExecuteRequestResult(
                OperationResult.FromError(error),
                HttpStatusCode.InternalServerError);
        }
    }

    public static async Task WriteResultAsync(
        IExecutionResult executionResult,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? statusCode,
        HttpContext context,
        ExecutorSession executorSession)
    {
        // query results use pooled memory a need to be disposed
        // after we are finished with hem.
        await using var result = executionResult;
        IDisposable? formatScope = null;

        try
        {
            // if cancellation is requested, we will not try to attempt to write the result to the
            // response stream.
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            // in any case, we will have a valid GraphQL result at this point that can be written
            // to the HTTP response stream.
            Debug.Assert(result is not null, "No GraphQL result was created.");

            if (result is OperationResult queryResult)
            {
                formatScope = executorSession.DiagnosticEvents.FormatHttpResponse(context, queryResult);
            }

            await executorSession.WriteResultAsync(context, result, acceptMediaTypes, statusCode);
        }
        finally
        {
            // last we dispose the diagnostic scope.
            formatScope?.Dispose();
        }
    }

    public readonly record struct ValidateAcceptContentTypeResult
    {
        public ValidateAcceptContentTypeResult(
            RequestFlags requestFlags,
            AcceptMediaType[] acceptMediaTypes)
        {
            IsValid = true;
            Error = null;
            StatusCode = null;
            RequestFlags = requestFlags;
            AcceptMediaTypes = acceptMediaTypes;
        }

        public ValidateAcceptContentTypeResult(
            OperationResult errorResult,
            HttpStatusCode statusCode)
        {
            IsValid = false;
            Error = errorResult;
            StatusCode = statusCode;
            RequestFlags = RequestFlags.None;
            AcceptMediaTypes = [];
        }

        public ValidateAcceptContentTypeResult(
            IError error,
            HttpStatusCode statusCode,
            AcceptMediaType[] acceptMediaTypes)
        {
            IsValid = false;
            Error = OperationResult.FromError(error);
            StatusCode = statusCode;
            RequestFlags = RequestFlags.None;
            AcceptMediaTypes = acceptMediaTypes;
        }

        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(false, nameof(StatusCode))]
        public bool IsValid { get; }

        public OperationResult? Error { get; }

        public HttpStatusCode? StatusCode { get; }

        public RequestFlags RequestFlags { get; }

        public AcceptMediaType[] AcceptMediaTypes { get; }
    }

    public readonly record struct ParseRequestResult
    {
        public ParseRequestResult(GraphQLRequest request)
        {
            IsValid = true;
            Request = request;
            Error = null;
            StatusCode = null;
        }

        public ParseRequestResult(OperationResult errorResult, HttpStatusCode? statusCode)
        {
            IsValid = false;
            Error = errorResult;
            StatusCode = statusCode;
            Request = null;
        }

        public ParseRequestResult(IError error, HttpStatusCode statusCode)
        {
            IsValid = false;
            Error = OperationResult.FromError(error);
            StatusCode = statusCode;
            Request = null;
        }

        [MemberNotNullWhen(true, nameof(Request))]
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(false, nameof(StatusCode))]
        public bool IsValid { get; }

        public GraphQLRequest? Request { get; }

        public OperationResult? Error { get; }

        public HttpStatusCode? StatusCode { get; }
    }

    public readonly record struct ExecuteRequestResult
    {
        public ExecuteRequestResult(IExecutionResult result, HttpStatusCode? statusCode = null)
        {
            Result = result;
            StatusCode = statusCode;
        }

        public IExecutionResult? Result { get; }

        public HttpStatusCode? StatusCode { get; }
    }
}
