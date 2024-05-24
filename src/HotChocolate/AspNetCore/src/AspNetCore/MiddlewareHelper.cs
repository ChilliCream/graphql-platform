using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class MiddlewareHelper
{
    public static ValidateAcceptContentTypeResult ValidateAcceptContentType(
        HttpContext context,
        IHttpResponseFormatter responseFormatter,
        IServerDiagnosticEvents diagnosticEvents)
    {
        // first  we will inspect the accept headers and determine if we can execute this request.
        var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);

        // if we cannot parse all media types that the user provided we will fail the request
        // with a 400 Bad Request.
        if (headerResult.HasError)
        {
            var errors = headerResult.ErrorResult.Errors!;
            diagnosticEvents.HttpRequestError(context, errors[0]);

            return new ValidateAcceptContentTypeResult(
                headerResult.ErrorResult,
                HttpStatusCode.BadRequest);
        }

        var requestFlags = responseFormatter.CreateRequestFlags(headerResult.AcceptMediaTypes);

        // if the request defines accept header values of which we cannot handle any provided
        // media type then we will fail the request with 406 Not Acceptable.
        if (requestFlags is GraphQLRequestFlags.None)
        {
            var error = ErrorHelper.NoSupportedAcceptMediaType();
            diagnosticEvents.HttpRequestError(context, error);

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
        IHttpRequestParser requestParser,
        IErrorHandler errorHandler,
        IServerDiagnosticEvents diagnosticEvents)
    {
        using (diagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                return new ParseRequestResult(
                    requestParser.ParseRequestFromParams(
                        context.Request.Query));
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = errorHandler.Handle(ex.Errors);
                diagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                diagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }
    }

    public static ParseRequestResult ParseVariablesAndExtensionsFromParams(
        string operationId,
        HttpContext context,
        IHttpRequestParser requestParser,
        IErrorHandler errorHandler,
        IServerDiagnosticEvents diagnosticEvents)
    {
        using (diagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                return new ParseRequestResult(
                    requestParser.ParsePersistedOperationRequestFromParams(
                        operationId,
                        context.Request.Query));
            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = errorHandler.Handle(ex.Errors);
                diagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                diagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }
    }

    public static async Task<ParseRequestResult> ParseSingleRequestFromBodyAsync(
        string operationId,
        HttpContext context,
        IHttpRequestParser requestParser,
        IErrorHandler errorHandler,
        IServerDiagnosticEvents diagnosticEvents)
    {
        GraphQLRequest request;
        using (diagnosticEvents.ParseHttpRequest(context))
        {
            try
            {
                request =
                    await requestParser.ParsePersistedOperationRequestAsync(
                        operationId,
                        context.Request.Body,
                        context.RequestAborted);

            }
            catch (GraphQLRequestException ex)
            {
                // A GraphQL request exception is thrown if the HTTP request body couldn't be
                // parsed. In this case we will return HTTP status code 400 and return a
                // GraphQL error result.
                var errors = errorHandler.Handle(ex.Errors);
                diagnosticEvents.ParserErrors(context, errors);
                return new ParseRequestResult(errors, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                diagnosticEvents.HttpRequestError(context, error);
                return new ParseRequestResult(error, HttpStatusCode.InternalServerError);
            }
        }

        return new ParseRequestResult(request);
    }

    public static GraphQLRequestFlags DetermineHttpGetRequestFlags(
        GraphQLRequestFlags requestFlags,
        GraphQLServerOptions options)
    {
        if (options is null or { AllowedGetOperations: AllowedGetOperations.Query, })
        {
            requestFlags = (requestFlags & GraphQLRequestFlags.AllowStreams) == GraphQLRequestFlags.AllowStreams
                ? GraphQLRequestFlags.AllowQuery | GraphQLRequestFlags.AllowStreams
                : GraphQLRequestFlags.AllowQuery;
        }
        else
        {
            var flags = options.AllowedGetOperations;
            var newRequestFlags = GraphQLRequestFlags.None;

            if ((flags & AllowedGetOperations.Query) == AllowedGetOperations.Query)
            {
                newRequestFlags |= GraphQLRequestFlags.AllowQuery;
            }

            if ((flags & AllowedGetOperations.Mutation) == AllowedGetOperations.Mutation)
            {
                newRequestFlags |= GraphQLRequestFlags.AllowMutation;
            }

            if ((flags & AllowedGetOperations.Subscription) == AllowedGetOperations.Subscription &&
                (requestFlags & GraphQLRequestFlags.AllowSubscription) == GraphQLRequestFlags.AllowSubscription)
            {
                newRequestFlags |= GraphQLRequestFlags.AllowSubscription;
            }

            if ((requestFlags & GraphQLRequestFlags.AllowStreams) == GraphQLRequestFlags.AllowStreams)
            {
                newRequestFlags |= GraphQLRequestFlags.AllowStreams;
            }

            requestFlags = newRequestFlags;
        }

        return requestFlags;
    }

    public static async Task<ExecuteRequestResult> ExecuteRequestAsync(
        GraphQLRequest request,
        GraphQLRequestFlags flags,
        HttpContext context,
        IRequestExecutor requestExecutor,
        IErrorHandler errorHandler,
        IServerDiagnosticEvents diagnosticEvents)
    {
        // after successfully parsing the request we now will attempt to execute the request.
        var requestInterceptor = requestExecutor.GetRequestInterceptor();

        try
        {
            diagnosticEvents.StartSingleRequest(context, request);

            var requestBuilder = OperationRequestBuilder.From(request);
            requestBuilder.SetFlags(flags);

            await requestInterceptor.OnCreateAsync(
                context,
                requestExecutor,
                requestBuilder,
                context.RequestAborted);

            var result = await requestExecutor.ExecuteAsync(
                requestBuilder.Build(),
                context.RequestAborted);

            return new ExecuteRequestResult(result);
        }
        catch (GraphQLException ex)
        {
            // This allows extensions to throw GraphQL exceptions in the GraphQL interceptor.
            // we let the serializer determine the status code.
            foreach (var error in ex.Errors)
            {
                diagnosticEvents.HttpRequestError(context, error);
            }

            return new ExecuteRequestResult(
                OperationResultBuilder.CreateError(ex.Errors));
        }
        catch (Exception ex)
        {
            var error = errorHandler.CreateUnexpectedError(ex).Build();
            diagnosticEvents.HttpRequestError(context, error);
            return new ExecuteRequestResult(
                OperationResultBuilder.CreateError(error),
                HttpStatusCode.InternalServerError);
        }
    }

    public static async Task WriteResultAsync(
        IExecutionResult executionResult,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? statusCode,
        HttpContext context,
        IHttpResponseFormatter responseFormatter,
        IServerDiagnosticEvents diagnosticEvents)
    {
        // query results use pooled memory an need to be disposed
        // after we are finished with hem.
        await using var result = executionResult;
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
                formatScope = diagnosticEvents.FormatHttpResponse(context, queryResult);
            }

            await responseFormatter.FormatAsync(
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
            GraphQLRequestFlags requestFlags,
            AcceptMediaType[] acceptMediaTypes)
        {
            IsValid = true;
            Error = null;
            StatusCode = null;
            RequestFlags = requestFlags;
            AcceptMediaTypes = acceptMediaTypes;
        }

        public ValidateAcceptContentTypeResult(
            IOperationResult errorResult,
            HttpStatusCode statusCode)
        {
            IsValid = false;
            Error = errorResult;
            StatusCode = statusCode;
            RequestFlags = GraphQLRequestFlags.None;
            AcceptMediaTypes = Array.Empty<AcceptMediaType>();
        }

        public ValidateAcceptContentTypeResult(
            IError error,
            HttpStatusCode statusCode,
            AcceptMediaType[] acceptMediaTypes)
        {
            IsValid = false;
            Error = OperationResultBuilder.CreateError(error);
            StatusCode = statusCode;
            RequestFlags = GraphQLRequestFlags.None;
            AcceptMediaTypes = acceptMediaTypes;
        }

        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(false, nameof(StatusCode))]
        public bool IsValid { get; }

        public IOperationResult? Error { get; }

        public HttpStatusCode? StatusCode { get; }

        public GraphQLRequestFlags RequestFlags { get; }

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
            Error = OperationResultBuilder.CreateError(errors);
            StatusCode = statusCode;
            Request = null;
        }

        public ParseRequestResult(IError error, HttpStatusCode statusCode)
        {
            IsValid = false;
            Error = OperationResultBuilder.CreateError(error);
            StatusCode = statusCode;
            Request = null;
        }

        [MemberNotNullWhen(true, nameof(Request))]
        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(false, nameof(StatusCode))]
        public bool IsValid { get; }

        public GraphQLRequest? Request { get; }

        public IOperationResult? Error { get; }

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
