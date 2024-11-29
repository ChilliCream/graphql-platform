using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
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
    private readonly ConcurrentDictionary<string, CachedSchemaOutput> _schemaCache = new(StringComparer.Ordinal);
    private readonly ITimeProvider _timeProvider;
    private readonly FormatInfo _defaultFormat;
    private readonly FormatInfo _graphqlResponseFormat;
    private readonly FormatInfo _multiPartFormat;
    private readonly FormatInfo _eventStreamFormat;
    private readonly FormatInfo _legacyFormat;

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
    /// <param name="timeProvider">
    /// The time provider.
    /// </param>
    public DefaultHttpResponseFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null,
        ITimeProvider? timeProvider = null)
        : this(
            new HttpResponseFormatterOptions
            {
                Json = new JsonResultFormatterOptions { Indented = indented, Encoder = encoder, },
            },
            timeProvider)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DefaultHttpResponseFormatter" />.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    /// <param name="timeProvider">
    /// The time provider.
    /// </param>
    public DefaultHttpResponseFormatter(HttpResponseFormatterOptions options, ITimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? new DefaultTimeProvider();

        var jsonFormatter = new JsonResultFormatter(options.Json);
        var multiPartFormatter = new MultiPartResultFormatter(jsonFormatter);
        var eventStreamResultFormatter = new EventStreamResultFormatter(options.Json);

        _graphqlResponseFormat = new FormatInfo(
            ContentType.GraphQLResponse,
            ResponseContentType.GraphQLResponse,
            jsonFormatter);
        _legacyFormat = new FormatInfo(
            ContentType.Json,
            ResponseContentType.Json,
            jsonFormatter);
        _multiPartFormat = new FormatInfo(
            ContentType.MultiPartMixed,
            ResponseContentType.MultiPartMixed,
            multiPartFormatter);
        _eventStreamFormat = new FormatInfo(
            ContentType.EventStream,
            ResponseContentType.EventStream,
            eventStreamResultFormatter);
        _defaultFormat = options.HttpTransportVersion is HttpTransportVersion.Legacy
            ? _legacyFormat
            : _graphqlResponseFormat;
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
        var format = TryGetFormatter(result, acceptMediaTypes);

        if (format is null)
        {
            // we should not hit this point except if a middleware did not validate the
            // GraphQL request flags which would indicate that there is no way to execute
            // the GraphQL request with the specified accept-header content types.
            throw ThrowHelper.Formatter_InvalidAcceptMediaType();
        }

        switch (result)
        {
            case IOperationResult operationResult:
            {
                var statusCode = (int)OnDetermineStatusCode(operationResult, format, proposedStatusCode);

                response.ContentType = format.ContentType;
                response.StatusCode = statusCode;

                if (result.ContextData is not null
                    && result.ContextData.TryGetValue(WellKnownContextData.CacheControlHeaderValue, out var value)
                    && value is CacheControlHeaderValue cacheControlHeaderValue)
                {
                    response.GetTypedHeaders().CacheControl = cacheControlHeaderValue;
                }

                if (result.ContextData is not null
                    && result.ContextData.TryGetValue(VaryHeaderValue, out var varyValue)
                    && varyValue is string varyHeaderValue)
                {
                    response.Headers.Vary = varyHeaderValue;
                }

                OnWriteResponseHeaders(operationResult, format, response.Headers);

                await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
                break;
            }

            case OperationResultBatch resultBatch:
            {
                var statusCode = (int)OnDetermineStatusCode(resultBatch, format, proposedStatusCode);

                response.ContentType = format.ContentType;
                response.StatusCode = statusCode;
                response.Headers.CacheControl = HttpHeaderValues.NoCache;
                OnWriteResponseHeaders(resultBatch, format, response.Headers);
                await response.Body.FlushAsync(cancellationToken);

                await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
                break;
            }

            case IResponseStream responseStream:
            {
                var statusCode = (int)OnDetermineStatusCode(responseStream, format, proposedStatusCode);

                response.ContentType = format.ContentType;
                response.StatusCode = statusCode;
                response.Headers.CacheControl = HttpHeaderValues.NoCache;
                OnWriteResponseHeaders(responseStream, format, response.Headers);
                await response.Body.FlushAsync(cancellationToken);

                await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
                break;
            }

            default:
                // we should not hit this point except in the case that we introduce a new
                // ExecutionResultKind and forget to update this method.
                throw ThrowHelper.Formatter_ResultKindNotSupported();
        }
    }

    public async ValueTask FormatAsync(
        HttpResponse response,
        ISchema schema,
        ulong version,
        CancellationToken cancellationToken)
    {
        var output = _schemaCache.GetOrAdd(schema.Name, Update);

        if (output.Version < version)
        {
            lock (_schemaCache)
            {
                if (!_schemaCache.TryGetValue(schema.Name, out output)
                    || output.Version < version)
                {
                    _schemaCache[schema.Name] = output = Update(schema.Name);
                }
            }
        }

        var memory = output.AsMemory();
        response.ContentType = ContentType.GraphQL;
        response.Headers.SetContentDisposition(output.FileName);
        response.Headers.ETag = output.ETag;
        response.Headers.LastModified = output.LastModifiedTime.ToString("R");
        response.Headers.CacheControl = "public, max-age=3600, must-revalidate";
        response.Headers.ContentLength = memory.Length;
        await response.Body.WriteAsync(memory, cancellationToken);
        return;

        CachedSchemaOutput Update(string _) => new(schema, version, _timeProvider.UtcNow);
    }


    /// <summary>
    /// Determines which status code shall be returned for this result.
    /// </summary>
    /// <param name="result">
    /// The <see cref="IOperationResult"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="proposedStatusCode">
    /// The proposed status code of the middleware.
    /// </param>
    /// <returns>
    /// Returns the <see cref="HttpStatusCode"/> that the formatter must use.
    /// </returns>
    protected virtual HttpStatusCode OnDetermineStatusCode(
        IOperationResult result,
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
        // respond with an OK status code.
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
            // a status code or indicated an error that should be interpreted as a status code.
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
                // if that is the case we will return a BadRequest status code (400).
                if (contextData.ContainsKey(ValidationErrors))
                {
                    return HttpStatusCode.BadRequest;
                }

                if (result.ContextData.ContainsKey(OperationNotAllowed))
                {
                    return HttpStatusCode.MethodNotAllowed;
                }
            }

            // if data is set then the execution as begun and has produced a result.
            // The result of executing GraphQL operation may contain partial data as
            // well as encountered errors. Errors that happen during execution of the
            // GraphQL operation typically become part of the result, as long as the
            // server is still able to produce a well-formed response.
            // Even null represents a valid response, in this case of a non-null propagation
            // that erased the result.
            if (result.IsDataSet)
            {
                return HttpStatusCode.OK;
            }

            // if data was never set the result not valid and execution has never started, and we return a 400
            // if the user did not override the status code with a different status code.
            return HttpStatusCode.BadRequest;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        throw ThrowHelper.Formatter_ResponseContentTypeNotSupported(format.ContentType);
    }

    /// <summary>
    /// Override to write response headers to the response message before
    /// the formatter starts writing the response body.
    /// </summary>
    /// <param name="result">
    /// The <see cref="IOperationResult"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="headers">
    /// The header dictionary.
    /// </param>
    protected virtual void OnWriteResponseHeaders(
        IOperationResult result,
        FormatInfo format,
        IHeaderDictionary headers)
    {
    }

    /// <summary>
    /// Determines which status code shall be returned for this response stream.
    /// </summary>
    /// <param name="responseStream">
    /// The <see cref="IResponseStream"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="proposedStatusCode">
    /// The proposed status code of the middleware.
    /// </param>
    /// <returns>
    /// Returns the <see cref="HttpStatusCode"/> that the formatter must use.
    /// </returns>
    protected virtual HttpStatusCode OnDetermineStatusCode(
        IResponseStream responseStream,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        // if we are sending a response stream with the multipart/mixed header or
        // with a text/event-stream response content-type we as well will just
        // respond with an OK status code.
        if (format.Kind is ResponseContentType.MultiPartMixed or ResponseContentType.EventStream)
        {
            return HttpStatusCode.OK;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        throw ThrowHelper.Formatter_ResponseContentTypeNotSupported(format.ContentType);
    }

    /// <summary>
    /// Override to write response headers to the response message before
    /// the formatter starts writing the response body.
    /// </summary>
    /// <param name="responseStream">
    /// The <see cref="IResponseStream"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="headers">
    /// The header dictionary.
    /// </param>
    protected virtual void OnWriteResponseHeaders(
        IResponseStream responseStream,
        FormatInfo format,
        IHeaderDictionary headers)
    {
    }

    /// <summary>
    /// Determines which status code shall be returned for a result batch.
    /// </summary>
    /// <param name="resultBatch">
    /// The <see cref="OperationResultBatch"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="proposedStatusCode">
    /// The proposed status code of the middleware.
    /// </param>
    /// <returns>
    /// Returns the <see cref="HttpStatusCode"/> that the formatter must use.
    /// </returns>
    protected virtual HttpStatusCode OnDetermineStatusCode(
        OperationResultBatch resultBatch,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
        => HttpStatusCode.OK;

    /// <summary>
    /// Override to write response headers to the response message before
    /// the formatter starts writing the response body.
    /// </summary>
    /// <param name="resultBatch">
    /// The <see cref="OperationResultBatch"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="headers">
    /// The header dictionary.
    /// </param>
    protected virtual void OnWriteResponseHeaders(
        OperationResultBatch resultBatch,
        FormatInfo format,
        IHeaderDictionary headers)
    {
    }

    private FormatInfo? TryGetFormatter(
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes)
    {
        var length = acceptMediaTypes.Length;

        // There is no Accept header present, so the server is allowed
        // to select what makes the most sense for the response.
        if (length == 0)
        {
            if (result.Kind is SingleResult)
            {
                return _defaultFormat;
            }

            if (result.Kind is DeferredResult or BatchResult)
            {
                return _multiPartFormat;
            }

            if (result.Kind is SubscriptionResult)
            {
                return _eventStreamFormat;
            }

            return null;
        }

        // if the request specifies at least one accept media-type we will
        // determine which is best to use.
        // For this we first determine which characteristics our GraphQL result has.
        var resultKind = result.Kind switch
        {
            SingleResult => ResultKind.Single,
            SubscriptionResult => ResultKind.Subscription,
            _ => ResultKind.Stream,
        };

        ref var start = ref MemoryMarshal.GetArrayDataReference(acceptMediaTypes);

        // If we just have one Accept header value we will try to determine which formatter to take.
        // We should only be unable to find a match if there was a previous validation skipped.
        if (length == 1)
        {
            var mediaType = start;

            if (resultKind is ResultKind.Single && mediaType.Kind is ApplicationGraphQL)
            {
                return _graphqlResponseFormat;
            }

            if (resultKind is ResultKind.Single && mediaType.Kind is ApplicationJson)
            {
                return _legacyFormat;
            }

            if (resultKind is ResultKind.Single && mediaType.Kind is AllApplication or All)
            {
                return _defaultFormat;
            }

            if (resultKind is ResultKind.Stream or ResultKind.Single
                && mediaType.Kind is MultiPartMixed or AllMultiPart or All)
            {
                return _multiPartFormat;
            }

            if (mediaType.Kind is EventStream or All)
            {
                return _eventStreamFormat;
            }

            return null;
        }

        // If we have more than one specified accept-header value we will try to find the best for
        // our GraphQL result.
        ref var end = ref Unsafe.Add(ref start, length);
        FormatInfo? possibleFormat = null;

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (resultKind is ResultKind.Single && start.Kind is AllApplication or All)
            {
                return _defaultFormat;
            }

            if (resultKind is ResultKind.Single && start.Kind is ApplicationJson)
            {
                // application/json is a legacy response content-type.
                // We will create a formatInfo but keep on validating for
                // a better suited format.
                possibleFormat = _legacyFormat;
            }

            if (resultKind is ResultKind.Single && start.Kind is ApplicationGraphQL)
            {
                return _graphqlResponseFormat;
            }

            if (resultKind is ResultKind.Stream or ResultKind.Single
                && start.Kind is MultiPartMixed or AllMultiPart or All)
            {
                // if the result is a stream we consider this a perfect match and
                // will use this format.
                if (resultKind is ResultKind.Stream)
                {
                    possibleFormat = _multiPartFormat;
                }

                // if the format is an event-stream or not set we will create a
                // multipart/mixed formatInfo for the current result but also keep
                // on validating for a better suited format.
                if (possibleFormat?.Kind is not ResponseContentType.Json)
                {
                    possibleFormat = _multiPartFormat;
                }
            }

            if (start.Kind is EventStream or All)
            {
                // if the result is a subscription we consider this a perfect match and
                // will use this format.
                if (resultKind is ResultKind.Subscription or ResultKind.Stream)
                {
                    possibleFormat = _eventStreamFormat;
                }

                // if the result is stream it means that we did not yet validate a
                // multipart content-type and thus will create a format for the case that it
                // is not specified;
                // or we have a single result but there is no format yet specified
                // we will create a text/event-stream formatInfo for the current result
                // but also keep on validating for a better suited format.
                if (possibleFormat?.Kind is ResponseContentType.Unknown)
                {
                    possibleFormat = _multiPartFormat;
                }
            }

            start = ref Unsafe.Add(ref start, 1);
        }

        return possibleFormat;
    }

    internal static DefaultHttpResponseFormatter Create(
        HttpResponseFormatterOptions options,
        ITimeProvider timeProvider)
        => new SealedDefaultHttpResponseFormatter(options, timeProvider);

    /// <summary>
    /// Representation of a resolver format, containing the formatter and the content type.
    /// </summary>
    protected sealed class FormatInfo
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
        /// This prop is an optimization that helps to avoid comparing strings.
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
        Subscription,
    }

    private sealed class SealedDefaultHttpResponseFormatter(
        HttpResponseFormatterOptions options,
        ITimeProvider timeProvider)
        : DefaultHttpResponseFormatter(options, timeProvider);

    private sealed class CachedSchemaOutput
    {
        private readonly byte[] _schema;

        public CachedSchemaOutput(ISchema schema, ulong version, DateTimeOffset lastModifiedTime)
        {
            _schema = Encoding.UTF8.GetBytes(schema.ToString());
            FileName = GetSchemaFileName(schema);
            ETag = CreateETag(_schema, version);
            LastModifiedTime = lastModifiedTime;
            Version = version;
        }

        public string FileName { get; }

        public string ETag { get; }

        public ulong Version { get; }

        public DateTimeOffset LastModifiedTime { get; }

        public ReadOnlyMemory<byte> AsMemory() => _schema;

        private static string CreateETag(byte[] schema, ulong version)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(schema);
            var hash = Convert.ToBase64String(hashBytes);
            return $"\"{version}-{hash}\"";
        }

        private static string GetSchemaFileName(ISchema schema)
            => schema.Name.EqualsOrdinal(Schema.DefaultName)
                ? "schema.graphql"
                : schema.Name + ".schema.graphql";
    }
}
