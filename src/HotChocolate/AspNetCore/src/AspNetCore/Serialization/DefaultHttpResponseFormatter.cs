using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using Microsoft.AspNetCore.Http;
#if !NET6_0_OR_GREATER
using Microsoft.Net.Http.Headers;
#endif
using static HotChocolate.AspNetCore.AcceptMediaTypeKind;
using static HotChocolate.Execution.ExecutionResultKind;
using static HotChocolate.WellKnownContextData;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// This represents the default implementation for the <see cref="IHttpResponseFormatter" />
/// that abides by the GraphQL over HTTP specification.
/// https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md
/// </summary>
public class DefaultHttpResponseFormatter : IHttpResponseFormatter
{
    private readonly JsonResultFormatter _jsonFormatter;
    private readonly MultiPartResultFormatter _multiPartFormatter;
    private readonly EventStreamResultFormatter _eventStreamResultFormatter;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultHttpResponseFormatter" />.
    /// </summary>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without extra white spaces.
    /// </param>
    /// <param name="encoder">
    /// Gets or sets the encoder to use when escaping strings, or null to use the default encoder.
    /// </param>
    public DefaultHttpResponseFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null)
        : this(new JsonResultFormatterOptions { Indented = indented, Encoder = encoder })
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DefaultHttpResponseFormatter" />.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public DefaultHttpResponseFormatter(JsonResultFormatterOptions options)
    {
        _jsonFormatter = new JsonResultFormatter(options);
        _multiPartFormatter = new MultiPartResultFormatter(_jsonFormatter);
        _eventStreamResultFormatter = new EventStreamResultFormatter(options);
    }

    public GraphQLRequestFlags CreateRequestFlags(
        AcceptMediaType[] acceptMediaTypes)
    {
        if (acceptMediaTypes.Length == 0)
        {
            return GraphQLRequestFlags.AllowLegacy;
        }

        var flags = GraphQLRequestFlags.None;

        ref var searchSpace = ref MemoryMarshal.GetReference(acceptMediaTypes.AsSpan());

        for (var i = 0; i < acceptMediaTypes.Length; i++)
        {
            var acceptMediaType = Unsafe.Add(ref searchSpace, i);
            flags |= CreateRequestFlags(acceptMediaType);

            if (flags is GraphQLRequestFlags.AllowAll)
            {
                return GraphQLRequestFlags.AllowAll;
            }
        }

        return flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual GraphQLRequestFlags CreateRequestFlags(
        AcceptMediaType acceptMediaType)
    {
        var flags = GraphQLRequestFlags.None;

        if (acceptMediaType.Kind is ApplicationGraphQL or ApplicationJson or AllApplication)
        {
            flags |= GraphQLRequestFlags.AllowQuery;
            flags |= GraphQLRequestFlags.AllowMutation;
        }

        if (acceptMediaType.Kind is MultiPartMixed or AllMultiPart)
        {
            flags |= GraphQLRequestFlags.AllowQuery;
            flags |= GraphQLRequestFlags.AllowMutation;
            flags |= GraphQLRequestFlags.AllowStreams;
        }

        if (acceptMediaType.Kind is EventStream or All)
        {
            flags = GraphQLRequestFlags.AllowAll;
        }

        return flags;
    }

    public async ValueTask FormatAsync(
        HttpResponse response,
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? proposedStatusCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetFormatter(result, acceptMediaTypes, out var format))
        {
            // we should not hit this point except if a middleware did not validate the
            // GraphQL request flags which would indicate that there is no way to execute
            // the GraphQL request with the specified accept header content types.
            throw ThrowHelper.Formatter_InvalidAcceptMediaType();
        }

        if (result.Kind is SingleResult)
        {
            var queryResult = (IQueryResult)result;
            var statusCode = (int)GetStatusCode(queryResult, format, proposedStatusCode);

            response.ContentType = format.ContentType;
            response.StatusCode = statusCode;

            if (result.ContextData is not null &&
                result.ContextData.TryGetValue(CacheControlHeaderValue, out var value) &&
                value is string cacheControlHeaderValue)
            {
#if NET6_0_OR_GREATER
                response.Headers.CacheControl = cacheControlHeaderValue;
#else
                response.Headers.Add(HeaderNames.CacheControl, cacheControlHeaderValue);
#endif
            }

            await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            var responseStream = (IResponseStream)result;
            var statusCode = (int)GetStatusCode(responseStream, format, proposedStatusCode);

            response.Headers.Add(HttpHeaderKeys.CacheControl, HttpHeaderValues.NoCache);
            response.ContentType = format.ContentType;
            response.StatusCode = statusCode;
            await response.Body.FlushAsync(cancellationToken);

            await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else
        {
            // we should not hit this point except in the case that we introduce a new
            // ExecutionResultKind and forget to update this method.
            throw ThrowHelper.Formatter_ResultKindNotSupported();
        }
    }

    protected virtual HttpStatusCode GetStatusCode(
        IQueryResult result,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        // the current spec proposal strongly recommend to always return OK
        // when using the legacy application/json response content-type.
        if (format.Kind is ResponseContentType.Json)
        {
            return HttpStatusCode.OK;
        }

        // if we are sending a single result with the multipart/mixed header or
        // with a text/event-stream response content-type we as well will just
        // respond with a OK status code.
        if (format.Kind is ResponseContentType.MultiPartMixed or ResponseContentType.EventStream)
        {
            return HttpStatusCode.OK;
        }

        // in the case of the application/graphql-response+json we will
        // use status code to indicate certain kinds of error categories.
        if (format.Kind is ResponseContentType.GraphQLResponse)
        {
            // if a status code was proposed by the middleware we will in general accept it.
            // the middleware are implement in a way that they will propose status code for
            // the application/graphql-response+json response content-type.
            if (proposedStatusCode.HasValue)
            {
                return proposedStatusCode.Value;
            }

            // if the GraphQL result has context data we will check if some middleware provided
            // a status code or indicated an error that should be interpreted as an status code.
            if (result.ContextData is not null)
            {
                var contextData = result.ContextData;

                // first we check if there is an explicit HTTP status code override by the user.
                if (contextData.TryGetValue(WellKnownContextData.HttpStatusCode, out var value))
                {
                    if (value is HttpStatusCode statusCode)
                    {
                        return statusCode;
                    }

                    if (value is int statusCodeInt)
                    {
                        return (HttpStatusCode)statusCodeInt;
                    }
                }

                // next we check if the validation of the request failed.
                // if that is the case we will we will return a BadRequest status code (400).
                if (contextData.ContainsKey(ValidationErrors))
                {
                    return HttpStatusCode.BadRequest;
                }

                if (result.ContextData.ContainsKey(OperationNotAllowed))
                {
                    return HttpStatusCode.MethodNotAllowed;
                }
            }

            // if data is not null then we have a valid result. The result of executing
            // a GraphQL operation may contain partial data as well as encountered errors.
            // Errors that happen during execution of the GraphQL operation typically
            // become part of the result, as long as the server is still able to produce
            // a well-formed response.
            if (result.Data is not null)
            {
                return HttpStatusCode.OK;
            }

            // if data is null we consider the result not valid and return a 500 if the user did
            // not override the status code with a different status code.
            // this is however at the moment a point of discussion as there are opposing views
            // towards what constitutes a valid response.
            // we will update this status code as the spec moves towards release.
            return HttpStatusCode.InternalServerError;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        throw ThrowHelper.Formatter_ResponseContentTypeNotSupported(format.ContentType);
    }

    protected virtual HttpStatusCode GetStatusCode(
        IResponseStream responseStream,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        // if we are sending a response stream with the multipart/mixed header or
        // with a text/event-stream response content-type we as well will just
        // respond with a OK status code.
        if (format.Kind is ResponseContentType.MultiPartMixed or ResponseContentType.EventStream)
        {
            return HttpStatusCode.OK;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        throw ThrowHelper.Formatter_ResponseContentTypeNotSupported(format.ContentType);
    }

    private bool TryGetFormatter(
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        out FormatInfo formatInfo)
    {
        formatInfo = default;

        // if the request does not specify the accept header then we will
        // use the `application/graphql-response+json` response content-type,
        // which is the new response content-type.
        if (acceptMediaTypes.Length == 0)
        {
            if (result.Kind is SingleResult)
            {
                formatInfo = new FormatInfo(
                    ContentType.GraphQLResponse,
                    ResponseContentType.GraphQLResponse,
                    _jsonFormatter);
                return true;
            }

            if (result.Kind is DeferredResult or BatchResult)
            {
                formatInfo = new FormatInfo(
                    ContentType.MultiPartMixed,
                    ResponseContentType.MultiPartMixed,
                    _multiPartFormatter);
                return true;
            }

            if (result.Kind is SubscriptionResult)
            {
                formatInfo = new FormatInfo(
                    ContentType.EventStream,
                    ResponseContentType.EventStream,
                    _eventStreamResultFormatter);
                return true;
            }

            return false;
        }

        // if the request specifies at least one accept media-type we will
        // determine which is best to use.
        // For this we first determine which characteristics our GraphQL result has.
        var resultKind = result.Kind switch
        {
            SingleResult => ResultKind.Single,
            SubscriptionResult => ResultKind.Subscription,
            _ => ResultKind.Stream
        };

        // if we just have one accept header we will try to determine which formatter to take.
        // we should only be unable to find a match if there was a previous validation skipped.
        if (acceptMediaTypes.Length == 1)
        {
            var mediaType = acceptMediaTypes[0];

            if (resultKind is ResultKind.Single &&
                mediaType.Kind is ApplicationGraphQL or AllApplication or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.GraphQLResponse,
                    ResponseContentType.GraphQLResponse,
                    _jsonFormatter);
                return true;
            }

            if (resultKind is ResultKind.Single &&
                mediaType.Kind is ApplicationJson)
            {
                formatInfo = new FormatInfo(
                    ContentType.Json,
                    ResponseContentType.Json,
                    _jsonFormatter);
                return true;
            }

            if (resultKind is ResultKind.Stream or ResultKind.Single &&
                mediaType.Kind is MultiPartMixed or AllMultiPart or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.MultiPartMixed,
                    ResponseContentType.MultiPartMixed,
                    _multiPartFormatter);
                return true;
            }

            if (mediaType.Kind is EventStream)
            {
                formatInfo = new FormatInfo(
                    ContentType.EventStream,
                    ResponseContentType.EventStream,
                    _eventStreamResultFormatter);
                return true;
            }

            return false;
        }

        // if we have more than one specified accept media-type we will try to find the best for
        // our GraphQL result.
        ref var searchSpace = ref MemoryMarshal.GetReference(acceptMediaTypes.AsSpan());
        var success = false;

        for (var i = 0; i < acceptMediaTypes.Length; i++)
        {
            var mediaType = Unsafe.Add(ref searchSpace, i);

            if (resultKind is ResultKind.Single &&
                mediaType.Kind is ApplicationGraphQL or AllApplication or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.GraphQLResponse,
                    ResponseContentType.GraphQLResponse,
                    _jsonFormatter);
                return true;
            }

            if (resultKind is ResultKind.Single &&
                mediaType.Kind is ApplicationJson)
            {
                // application/json is a legacy response content-type.
                // We will create a formatInfo but keep on validating for
                // a better suited format.
                formatInfo = new FormatInfo(
                    ContentType.Json,
                    ResponseContentType.Json,
                    _jsonFormatter);
                success = true;
            }

            if (resultKind is ResultKind.Stream or ResultKind.Single &&
                mediaType.Kind is MultiPartMixed or AllMultiPart or All)
            {
                // if the result is a stream we consider this a perfect match and
                // will use this format.
                if (resultKind is ResultKind.Stream)
                {
                    formatInfo = new FormatInfo(
                        ContentType.MultiPartMixed,
                        ResponseContentType.MultiPartMixed,
                        _multiPartFormatter);
                    return true;
                }

                // if the format is a event-stream or not set we will create a
                // multipart/mixed formatInfo for the current result but also keep
                // on validating for a better suited format.
                if (formatInfo.Kind is not ResponseContentType.Json)
                {
                    formatInfo = new FormatInfo(
                        ContentType.MultiPartMixed,
                        ResponseContentType.MultiPartMixed,
                        _multiPartFormatter);
                    success = true;
                }
            }

            if (mediaType.Kind is EventStream or All)
            {
                // if the result is a subscription we consider this a perfect match and
                // will use this format.
                if (resultKind is ResultKind.Stream)
                {
                    formatInfo = new FormatInfo(
                        ContentType.EventStream,
                        ResponseContentType.EventStream,
                        _eventStreamResultFormatter);
                    return true;
                }

                // if the result is stream it means that we did not yet validated a
                // multipart content-type and thus will create a format for the case that it
                // is not specified;
                // or we have a single result but there is no format yet specified
                // we will create a text/event-stream formatInfo for the current result
                // but also keep on validating for a better suited format.
                if (formatInfo.Kind is ResponseContentType.Unknown)
                {
                    formatInfo = new FormatInfo(
                        ContentType.MultiPartMixed,
                        ResponseContentType.MultiPartMixed,
                        _multiPartFormatter);
                    success = true;
                }
            }
        }

        return success;
    }

    /// <summary>
    /// Representation of a resolver format, containing the formatter and the content type.
    /// </summary>
    protected readonly struct FormatInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormatInfo"/>.
        /// </summary>
        public FormatInfo(
            string contentType,
            ResponseContentType kind,
            IExecutionResultFormatter formatter)
        {
            ContentType = contentType;
            Kind = kind;
            Formatter = formatter;
        }

        /// <summary>
        /// Gets the response content type.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets an enum value representing well-known response content types.
        /// This prop is an optimization that helps avoiding comparing strings.
        /// </summary>
        public ResponseContentType Kind { get; }

        /// <summary>
        /// Gets the formatter that creates the body of the HTTP response.
        /// </summary>
        public IExecutionResultFormatter Formatter { get; }
    }

    private enum ResultKind
    {
        Single,
        Stream,
        Subscription
    }
}
