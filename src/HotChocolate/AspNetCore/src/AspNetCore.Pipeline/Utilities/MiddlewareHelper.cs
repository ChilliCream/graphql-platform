using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
            var errors = headerResult.ErrorResult.Errors!;
            executorSession.DiagnosticEvents.HttpRequestError(context, errors[0]);

            return new ValidateAcceptContentTypeResult(
                headerResult.ErrorResult,
                HttpStatusCode.BadRequest);
        }

        var requestFlags = executorSession.ResponseFormatter.CreateRequestFlags(headerResult.AcceptMediaTypes);

        // if the request defines accept header values of which we cannot handle any provided
        // media type, then we will fail the request with 406 Not Acceptable.
        if (requestFlags is RequestFlags.None)
        {
            var error = ErrorHelper.NoSupportedAcceptMediaType();
            executorSession.DiagnosticEvents.HttpRequestError(context, error);

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
                var request = executorSession.RequestParser.ParseRequestFromParams(context.Request.Query);
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
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
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
                    executorSession.RequestParser.ParsePersistedOperationRequestFromParams(
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
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
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
                    await executorSession.RequestParser.ParsePersistedOperationRequestAsync(
                        operationId,
                        operationName,
                        context.Request.BodyReader,
                        context.RequestAborted);
                context.Response.RegisterForDispose(request);
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case, we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = executorSession.Handle(ex.Errors);
                executorSession.DiagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
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

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
                OperationResult.FromError([..ex.Errors]));
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

            await executorSession.ResponseFormatter.FormatAsync(
                context.Response,
                result,
                acceptMediaTypes,
                statusCode,
                context.RequestAborted);
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

        public ParseRequestResult(IReadOnlyList<IError> errors, HttpStatusCode statusCode)
        {
            IsValid = false;
            Error = OperationResult.FromError([..errors]);
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
