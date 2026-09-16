using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Serialization;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using static HotChocolate.AspNetCore.AcceptMediaTypeKind;
using static HotChocolate.Execution.ExecutionResultKind;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This represents the default implementation for the <see cref="IHttpResponseFormatter" />
/// that abides by the GraphQL over HTTP specification.
/// https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md
/// </summary>
public class DefaultHttpResponseFormatter : IHttpResponseFormatter
{
    private const HttpTransportVersion LatestTransportVersion = HttpTransportVersion.Draft20250508;

    private readonly ConcurrentDictionary<string, CachedSchemaOutput> _schemaCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, CachedSemanticNonNullSchemaOutput> _semanticNonNullSchemaCache = new(StringComparer.Ordinal);
    private readonly ITimeProvider _timeProvider;
    private readonly FormatInfo _defaultFormat;
    private readonly FormatInfo _graphqlResponseFormat;
    private readonly FormatInfo _graphqlResponseStreamFormat;
    private readonly FormatInfo _multiPartFormat;
    private readonly FormatInfo _eventStreamFormat;
    private readonly FormatInfo _jsonLinesFormat;
    private readonly FormatInfo _legacyFormat;
    private readonly FormatInfo[] _singleFormats;
    private readonly FormatInfo[] _streamFormats;
    private readonly FormatInfo[] _subscriptionFormats;
    private readonly FormatInfo[] _singlePreferred;
    private readonly FormatInfo[] _streamPreferred;
    private readonly IncrementalDeliveryFormat _incrementalDeliveryDefaultFormat;

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
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the Accept header does not specify one.
    /// </param>
    public DefaultHttpResponseFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null,
        ITimeProvider? timeProvider = null,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
        : this(
            new HttpResponseFormatterOptions
            {
                Json = new JsonResultFormatterOptions
                {
                    Indented = indented,
                    Encoder = encoder
                }
            },
            timeProvider,
            incrementalDeliveryFormat)
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
    /// <param name="incrementalDeliveryFormat">
    /// The default incremental delivery format to use when the Accept header does not specify one.
    /// </param>
    public DefaultHttpResponseFormatter(
        HttpResponseFormatterOptions options,
        ITimeProvider? timeProvider = null,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Version_0_2)
    {
        _timeProvider = timeProvider ?? new DefaultTimeProvider();

        var jsonFormatter = new JsonResultFormatter(options.Json);
        var multiPartFormatter = new MultiPartResultFormatter(jsonFormatter);
        var eventStreamResultFormatter = new EventStreamResultFormatter(options.Json);
        var jsonLinesResultFormatter = new JsonLinesResultFormatter(options.Json);

        _graphqlResponseFormat = new FormatInfo(
            ContentType.GraphQLResponse,
            ResponseContentType.GraphQLResponse,
            jsonFormatter);
        _graphqlResponseStreamFormat = new FormatInfo(
            ContentType.GraphQLResponseStream,
            ResponseContentType.GraphQLResponseStream,
            jsonLinesResultFormatter);
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
        _jsonLinesFormat = new FormatInfo(
            ContentType.JsonLines,
            ResponseContentType.JsonLines,
            jsonLinesResultFormatter);
        TransportVersion = ResolveTransportVersion(options.HttpTransportVersion, nameof(options));
        _defaultFormat = TransportVersion is HttpTransportVersion.Legacy
            ? _legacyFormat
            : _graphqlResponseFormat;

        // The formats the server can produce for each result kind, in the order it prefers them.
        // A tie on quality is resolved by this order.
        _singleFormats =
        [
            _graphqlResponseFormat,
            _legacyFormat,
            _multiPartFormat,
            _eventStreamFormat
        ];
        _streamFormats =
        [
            _graphqlResponseStreamFormat,
            _jsonLinesFormat,
            _multiPartFormat,
            _eventStreamFormat
        ];
        _subscriptionFormats =
        [
            _graphqlResponseStreamFormat,
            _jsonLinesFormat,
            _eventStreamFormat
        ];

        // Naming one of these outright is a request the server grants as it stands. Every other
        // format it can produce is a fallback, never something the client's ordering promotes.
        _singlePreferred = [_graphqlResponseFormat];
        _streamPreferred = [_graphqlResponseStreamFormat, _jsonLinesFormat];

        _incrementalDeliveryDefaultFormat = incrementalDeliveryFormat is IncrementalDeliveryFormat.Undefined
            ? IncrementalDeliveryFormat.Version_0_2
            : incrementalDeliveryFormat;
    }

    /// <summary>
    /// Gets the transport version the formatter writes responses against, with
    /// <see cref="HttpTransportVersion.Latest"/> resolved to the revision it stands for.
    /// </summary>
    internal HttpTransportVersion TransportVersion { get; }

    public RequestFlags CreateRequestFlags(
        AcceptMediaType[] acceptMediaTypes)
    {
        if (acceptMediaTypes.Length == 0)
        {
            return RequestFlags.AllowLegacy;
        }

        var flags = RequestFlags.None;

        ref var searchSpace = ref MemoryMarshal.GetReference(acceptMediaTypes.AsSpan());

        for (var i = 0; i < acceptMediaTypes.Length; i++)
        {
            var acceptMediaType = Unsafe.Add(ref searchSpace, i);

            // RFC 9110, section 12.4.2: a media type with q=0 is not acceptable. Excluding it
            // here leaves the request with no usable media type, which the middleware answers
            // with 406 before a response is ever formatted.
            if (GetQuality(acceptMediaType) is 0)
            {
                continue;
            }

            flags |= CreateRequestFlags(acceptMediaType);

            if (flags is RequestFlags.AllowAll)
            {
                return RequestFlags.AllowAll;
            }
        }

        return flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual RequestFlags CreateRequestFlags(
        AcceptMediaType acceptMediaType)
    {
        var flags = RequestFlags.None;

        if (acceptMediaType.Kind is ApplicationGraphQL or ApplicationJson or AllApplication)
        {
            flags |= RequestFlags.AllowQuery;
            flags |= RequestFlags.AllowMutation;
        }

        if (acceptMediaType.Kind is MultiPartMixed or AllMultiPart)
        {
            flags |= RequestFlags.AllowQuery;
            flags |= RequestFlags.AllowMutation;
            flags |= RequestFlags.AllowStreams;
        }

        if (acceptMediaType.Kind
            is ApplicationGraphQLStream or EventStream or AllText or ApplicationJsonLines or All)
        {
            flags = RequestFlags.AllowAll;
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
        var resultToWrite = result;
        var statusCode =
            ProposeStatusCodeForRefusedOperationKind(result, acceptMediaTypes, proposedStatusCode);
        OperationResult? notAcceptable = null;

        if (!TryGetFormatter(result, acceptMediaTypes, out var selectedAcceptMediaType, out var format))
        {
            // The request flags are validated before the operation runs, but they cannot know
            // which result kind it will produce, so an Accept header that excludes every format
            // this kind can be written in only becomes visible here. RFC 9110, section 15.5.7
            // answers that with a 406, and section 15.5.7 only recommends content rather than
            // requiring it. The error is written in the server's default format while the client
            // still accepts that format, and the status stands alone once it does not: a result
            // kind can be unwritable while a plain error remains readable, as a deferred result
            // is for a client that rejects every streaming media type but not application/json.
            if (MatchFormat(acceptMediaTypes, _defaultFormat.Kind).Quality is 0)
            {
                response.StatusCode = (int)(proposedStatusCode ?? HttpStatusCode.NotAcceptable);
                return;
            }

            notAcceptable = OperationResult.FromError(ErrorHelper.NoSupportedAcceptMediaType());
            resultToWrite = notAcceptable;
            selectedAcceptMediaType = default;
            format = _defaultFormat;
            statusCode = proposedStatusCode ?? HttpStatusCode.NotAcceptable;
        }

        try
        {
            await FormatInternalAsync(
                response,
                resultToWrite,
                statusCode,
                format,
                selectedAcceptMediaType,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // if the request is aborted, we will fail gracefully.
        }
        finally
        {
            if (notAcceptable is not null)
            {
                await notAcceptable.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Resolves the status code for an operation kind the executor refused. RFC 9110, section
    /// 15.5.6 scopes a 405 to a method the target resource does not support, so a refusal no
    /// change of method can resolve is a 406 instead. An operation kind the client's own
    /// <c>Accept</c> header never granted is such a refusal, because every other method carries
    /// the same header and is refused alike.
    /// </summary>
    private HttpStatusCode? ProposeStatusCodeForRefusedOperationKind(
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? proposedStatusCode)
    {
        if (proposedStatusCode.HasValue
            || result.ContextData is not { } contextData
            || !contextData.TryGetValue(ExecutionContextData.OperationNotAllowed, out var value)
            || value is not RequestFlags requiredFlag)
        {
            return proposedStatusCode;
        }

        return (CreateRequestFlags(acceptMediaTypes) & requiredFlag) == requiredFlag
            ? proposedStatusCode
            : HttpStatusCode.NotAcceptable;
    }

    private async ValueTask FormatInternalAsync(
        HttpResponse response,
        IExecutionResult result,
        HttpStatusCode? proposedStatusCode,
        FormatInfo format,
        AcceptMediaType acceptMediaType,
        CancellationToken cancellationToken)
    {
        var formatFlags = ResolveResultFormatFlags(acceptMediaType);

        switch (result)
        {
            case OperationResult operationResult:
            {
                var statusCode = (int)OnDetermineStatusCode(operationResult, format, proposedStatusCode);

                response.ContentType = format.ContentType;
                response.StatusCode = statusCode;

                // RFC 9110, section 15.5.6 requires a 405 to list the methods the target resource
                // supports, and section 10.2.1 defines that set per request. A GET or HEAD
                // carrying an operation kind this server only serves over POST leaves POST as the
                // one method that can satisfy it. A status code an overriding formatter chose is
                // left alone, along with whatever Allow header it means to write for it.
                if (statusCode is (int)HttpStatusCode.MethodNotAllowed
                    && result.ContextData.ContainsKey(ExecutionContextData.OperationNotAllowed)
                    && response.HttpContext.Request.IsGetOrHeadMethod())
                {
                    response.Headers.Allow = HttpMethods.Post;
                }

                if (result.ContextData.TryGetValue(ExecutionContextData.CacheControlHeaderValue, out var value)
                    && value is CacheControlHeaderValue cacheControlHeaderValue)
                {
                    response.GetTypedHeaders().CacheControl = cacheControlHeaderValue;
                }

                if (result.ContextData.TryGetValue(ExecutionContextData.VaryHeaderValue, out var varyValue)
                    && varyValue is string varyHeaderValue)
                {
                    response.Headers.Vary = varyHeaderValue;
                }

                OnWriteResponseHeaders(operationResult, format, response.Headers);

                await format.Formatter.FormatAsync(
                    result,
                    response.BodyWriter,
                    formatFlags,
                    cancellationToken: cancellationToken);
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

                await format.Formatter.FormatAsync(
                    result,
                    response.BodyWriter,
                    formatFlags,
                    cancellationToken: cancellationToken);
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

                await format.Formatter.FormatAsync(
                    result,
                    response.BodyWriter,
                    formatFlags,
                    cancellationToken: cancellationToken);
                break;
            }

            default:
                // we should not hit this point except in the case that we introduce a new
                // ExecutionResultKind and forget to update this method.
                throw ThrowHelper.Formatter_ResultKindNotSupported();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ExecutionResultFormatFlags ResolveResultFormatFlags(
        AcceptMediaType acceptMediaType)
    {
        var format = acceptMediaType.IncrementalDeliveryFormat is IncrementalDeliveryFormat.Undefined
            ? _incrementalDeliveryDefaultFormat
            : acceptMediaType.IncrementalDeliveryFormat;

        return format is IncrementalDeliveryFormat.Version_0_1
            ? ExecutionResultFormatFlags.IncrementalRfc1
            : ExecutionResultFormatFlags.None;
    }

    public async ValueTask FormatAsync(
        HttpResponse response,
        ISchemaDefinition schema,
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
        response.Headers.LastModified = output.LastModified;
        response.Headers.CacheControl = "public, max-age=3600, must-revalidate";
        response.Headers.ContentLength = memory.Length;
        await response.Body.WriteAsync(memory, cancellationToken);
        return;

        CachedSchemaOutput Update(string _)
            => new(schema, version, _timeProvider.UtcNow);
    }

    public async ValueTask FormatSemanticNonNullSchemaAsync(
        HttpResponse response,
        ISchemaDefinition schema,
        ulong version,
        CancellationToken cancellationToken)
    {
        var output = _semanticNonNullSchemaCache.GetOrAdd(schema.Name, Update);

        if (output.Version < version)
        {
            lock (_semanticNonNullSchemaCache)
            {
                if (!_semanticNonNullSchemaCache.TryGetValue(schema.Name, out output)
                    || output.Version < version)
                {
                    _semanticNonNullSchemaCache[schema.Name] = output = Update(schema.Name);
                }
            }
        }

        var memory = output.AsMemory();
        response.ContentType = ContentType.GraphQL;
        response.Headers.SetContentDisposition(output.FileName);
        response.Headers.ETag = output.ETag;
        response.Headers.LastModified = output.LastModified;
        response.Headers.CacheControl = "public, max-age=3600, must-revalidate";
        response.Headers.ContentLength = memory.Length;
        await response.Body.WriteAsync(memory, cancellationToken);
        return;

        CachedSemanticNonNullSchemaOutput Update(string _)
            => new(schema, version, _timeProvider.UtcNow);
    }

    /// <summary>
    /// Determines which status code shall be returned for this result.
    /// </summary>
    /// <param name="result">
    /// The <see cref="OperationResult"/>.
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
        OperationResult result,
        FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        if (format.Kind is ResponseContentType.Json)
        {
            // the legacy transport preserves the pre-spec behavior of always returning
            // 200 for the application/json response content-type.
            if (TransportVersion is HttpTransportVersion.Legacy)
            {
                return HttpStatusCode.OK;
            }

            // per graphql-over-http §6.4.1, the application/json response content-type
            // should return 200 for every well-formed request regardless of errors
            // raised. the only 4xx is 400 for requests the server cannot interpret
            // (§6.4.1.1.1 JSON parse, §6.4.1.1.2 invalid parameters). honor a proposed
            // 400; everything else, including an unexpected 500, stays 200.
            return proposedStatusCode is HttpStatusCode.BadRequest
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.OK;
        }

        // if we are sending a single result with the multipart/mixed header or
        // with a text/event-stream response content-type, we as well will just
        // respond with an OK status code.
        if (format.Kind is ResponseContentType.MultiPartMixed or ResponseContentType.EventStream)
        {
            return HttpStatusCode.OK;
        }

        // in the case of the application/graphql-response+json, we will
        // use status code to indicate certain kinds of error categories.
        if (format.Kind is ResponseContentType.GraphQLResponse)
        {
            // if a status code was proposed by the middleware, we will in general accept it.
            // the middleware is implemented in a way that they will propose status code for
            // the application/graphql-response+json response content-type.
            if (proposedStatusCode.HasValue)
            {
                return proposedStatusCode.Value;
            }

            // if the GraphQL result has context data, we will check if some middleware provided
            // a status code or indicated an error that should be interpreted as a status code.
            if (result.ContextData is { Count: > 0 } contextData)
            {
                // First, we check if there is an explicit HTTP status code override by the user.
                if (contextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value))
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

                // Next, we check if the validation of the request failed.
                // If that is the case, we will return a BadRequest status code (400).
                if (contextData.ContainsKey(ExecutionContextData.ValidationErrors))
                {
                    return HttpStatusCode.BadRequest;
                }

                if (contextData.ContainsKey(ExecutionContextData.OperationNotAllowed))
                {
                    return HttpStatusCode.MethodNotAllowed;
                }
            }

            // If data is set, then the execution as begun and has produced a result.
            // The result of executing GraphQL operation may contain partial data as
            // well as encountered errors. Errors that happen during execution of the
            // GraphQL operation typically become part of the result, as long as the
            // server is still able to produce a well-formed response.
            // Even null represents a valid response, in this case of a non-null propagation
            // that erased the result.
            if (result.Data.HasValue)
            {
                return HttpStatusCode.OK;
            }

            // if data was never set the result not valid and execution has never started, and we return a 400
            // if the user did not override the status code with a different status code.
            return HttpStatusCode.BadRequest;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here, the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        throw ThrowHelper.Formatter_ResponseContentTypeNotSupported(format.ContentType);
    }

    /// <summary>
    /// Override to write response headers to the response message before
    /// the formatter starts writing the response body.
    /// </summary>
    /// <param name="result">
    /// The <see cref="OperationResult"/>.
    /// </param>
    /// <param name="format">
    /// Provides information about the transport format that is applied.
    /// </param>
    /// <param name="headers">
    /// The header dictionary.
    /// </param>
    protected virtual void OnWriteResponseHeaders(
        OperationResult result,
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
        // with a text/event-stream response content-type, we as well will just
        // respond with an OK status code.
        if (format.Kind is ResponseContentType.MultiPartMixed
            or ResponseContentType.EventStream
            or ResponseContentType.GraphQLResponseStream
            or ResponseContentType.JsonLines)
        {
            return HttpStatusCode.OK;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here, the user did not fully implement all necessary parts to add support
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

    private bool TryGetFormatter(
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        out AcceptMediaType selectedAcceptMediaType,
        [NotNullWhen(true)] out FormatInfo? format)
    {
        selectedAcceptMediaType = default;
        format = null;

        // There is no Accept header present, so the server is allowed
        // to select what makes the most sense for the response.
        if (acceptMediaTypes.Length == 0)
        {
            format = result.Kind switch
            {
                SingleResult => _defaultFormat,
                DeferredResult or BatchResult => _multiPartFormat,
                SubscriptionResult => _eventStreamFormat,
                _ => null
            };

            return format is not null;
        }

        var candidates = result.Kind switch
        {
            SingleResult => _singleFormats,
            SubscriptionResult => _subscriptionFormats,
            _ => _streamFormats
        };

        // The format each result kind falls back to, which is the one chosen above for a request
        // that carries no Accept header at all. It settles ties between formats that only a
        // wildcard matched, because such a header names none of them in particular.
        var wildcardDefault = result.Kind switch
        {
            SingleResult => _defaultFormat,
            SubscriptionResult => _eventStreamFormat,
            _ => _multiPartFormat
        };

        var preferred = result.Kind is SingleResult ? _singlePreferred : _streamPreferred;

        // Every format the server can produce for this result is scored against the header, on
        // three keys in order.
        //
        // Quality first, per RFC 9110, sections 12.4.2 and 12.5.1. Scoring the server's formats
        // rather than walking the client's ranges is what lets a specific range override a
        // wildcard in both directions, whether it raises or removes a format.
        //
        // Then whether the client asked for the format or merely allowed it. Naming a preferred
        // format outright is a request, and so is a wildcard, which asks for the default; any
        // other match is a fallback.
        //
        // Then, between two requests, the one the client wrote first. Section 12.5.1 gives that
        // order no meaning, so this is the server's own choice among equally acceptable media
        // types, and it keeps a named format from losing to a wildcard beside it.
        var bestQuality = 0d;
        var bestRequested = false;
        var bestPosition = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var match = MatchFormat(acceptMediaTypes, candidate.Kind);

            if (match.Quality is 0)
            {
                continue;
            }

            var named = match.NamedPosition >= 0 && Contains(preferred, candidate);
            var byWildcard =
                match.WildcardPosition >= 0 && ReferenceEquals(candidate, wildcardDefault);
            var position = int.MaxValue;

            if (named)
            {
                position = match.NamedPosition;
            }

            if (byWildcard && match.WildcardPosition < position)
            {
                position = match.WildcardPosition;
            }

            var requested = named || byWildcard;

            if (match.Quality > bestQuality
                || (match.Quality.Equals(bestQuality)
                    && requested
                    && (!bestRequested || position < bestPosition)))
            {
                bestQuality = match.Quality;
                bestRequested = requested;
                bestPosition = position;
                selectedAcceptMediaType = acceptMediaTypes[match.RangeIndex];
                format = candidate;
            }
        }

        return format is not null;
    }

    private static bool Contains(FormatInfo[] formats, FormatInfo format)
    {
        foreach (var candidate in formats)
        {
            if (ReferenceEquals(candidate, format))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches one response content type against the header. The quality comes from the media
    /// range with the highest precedence that matches it, per RFC 9110, section 12.5.1, where a
    /// specific media type outranks <c>type/*</c>, which outranks <c>*/*</c>; a quality of zero
    /// means not acceptable, per section 12.4.2. The two positions are reported separately
    /// because a format can be both named and covered by a wildcard, and each carries a
    /// different request from the client.
    /// </summary>
    private static FormatMatch MatchFormat(
        AcceptMediaType[] acceptMediaTypes,
        ResponseContentType contentType)
    {
        var exactKind = GetExactKind(contentType);
        var wildcardKind = GetWildcardKind(contentType);
        var precedence = 0;
        var quality = 0d;
        var namedPosition = -1;
        var wildcardPosition = -1;
        var rangeIndex = -1;

        for (var i = 0; i < acceptMediaTypes.Length; i++)
        {
            ref readonly var acceptMediaType = ref acceptMediaTypes[i];
            int candidate;

            if (acceptMediaType.Kind == exactKind)
            {
                candidate = ExactRange;
            }
            else if (acceptMediaType.Kind == wildcardKind)
            {
                candidate = TypeWildcardRange;
            }
            else if (acceptMediaType.Kind is All)
            {
                candidate = FullWildcardRange;
            }
            else
            {
                continue;
            }

            if (candidate is ExactRange)
            {
                if (namedPosition < 0)
                {
                    namedPosition = i;
                }
            }
            else if (wildcardPosition < 0)
            {
                wildcardPosition = i;
            }

            if (candidate < precedence)
            {
                continue;
            }

            var candidateQuality = GetQuality(acceptMediaType);

            if (candidate > precedence || candidateQuality > quality)
            {
                precedence = candidate;
                quality = candidateQuality;
                rangeIndex = i;
            }
        }

        return new FormatMatch(quality, namedPosition, wildcardPosition, rangeIndex);
    }

    /// <summary>
    /// How one response content type fared against the client's Accept header. The range the
    /// quality came from is carried as an index into the header, <c>-1</c> when none matched,
    /// so that scoring a format does not copy an <see cref="AcceptMediaType"/>.
    /// </summary>
    private readonly record struct FormatMatch(
        double Quality,
        int NamedPosition,
        int WildcardPosition,
        int RangeIndex);

    /// <summary>
    /// Gets the media range that names a response content type exactly.
    /// </summary>
    private static AcceptMediaTypeKind GetExactKind(ResponseContentType contentType)
        => contentType switch
        {
            ResponseContentType.GraphQLResponse => ApplicationGraphQL,
            ResponseContentType.GraphQLResponseStream => ApplicationGraphQLStream,
            ResponseContentType.Json => ApplicationJson,
            ResponseContentType.JsonLines => ApplicationJsonLines,
            ResponseContentType.MultiPartMixed => MultiPartMixed,
            ResponseContentType.EventStream => EventStream,
            _ => Unknown
        };

    /// <summary>
    /// Gets the <c>type/*</c> range that covers a response content type.
    /// </summary>
    private static AcceptMediaTypeKind GetWildcardKind(ResponseContentType contentType)
        => contentType switch
        {
            ResponseContentType.MultiPartMixed => AllMultiPart,
            ResponseContentType.EventStream => AllText,
            _ => AllApplication
        };

    /// <summary>
    /// The media-range precedence levels of RFC 9110, section 12.5.1, from most to least
    /// specific: a named media type, then <c>type/*</c>, then <c>*/*</c>.
    /// </summary>
    private const int ExactRange = 3;
    private const int TypeWildcardRange = 2;
    private const int FullWildcardRange = 1;

    private static double GetQuality(AcceptMediaType mediaType)
        => mediaType.Quality ?? 1.0;

    private static HttpTransportVersion ResolveTransportVersion(
        HttpTransportVersion version,
        string paramName)
        => version switch
        {
            HttpTransportVersion.Latest => LatestTransportVersion,
            HttpTransportVersion.Legacy => HttpTransportVersion.Legacy,
            HttpTransportVersion.Draft20230127 => HttpTransportVersion.Draft20250508,
            HttpTransportVersion.Draft20250508 => HttpTransportVersion.Draft20250508,
            _ => throw ThrowHelper.Formatter_TransportVersionNotSupported(paramName, version)
        };

    internal static DefaultHttpResponseFormatter Create(
        HttpResponseFormatterOptions options,
        ITimeProvider timeProvider,
        IncrementalDeliveryFormat incrementalDeliveryFormat)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return new SealedDefaultHttpResponseFormatter(options, timeProvider, incrementalDeliveryFormat);
    }

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
        Subscription
    }

    private sealed class SealedDefaultHttpResponseFormatter(
        HttpResponseFormatterOptions options,
        ITimeProvider timeProvider,
        IncrementalDeliveryFormat incrementalDeliveryFormat)
        : DefaultHttpResponseFormatter(options, timeProvider, incrementalDeliveryFormat);

    private sealed class CachedSchemaOutput
    {
        private readonly byte[] _schema;

        public CachedSchemaOutput(ISchemaDefinition schema, ulong version, DateTimeOffset lastModifiedTime)
        {
            _schema = Encoding.UTF8.GetBytes(
                SchemaFormatter.FormatAsString(
                    schema,
                    new SchemaFormatterOptions
                    {
                        IncludeInternalDirectives = false
                    }));
            FileName = GetSchemaFileName(schema);
            ETag = CreateETag(_schema, version);
            LastModified = lastModifiedTime.ToString("R");
            Version = version;
        }

        public string FileName { get; }

        public string ETag { get; }

        public ulong Version { get; }

        public string LastModified { get; }

        public ReadOnlyMemory<byte> AsMemory() => _schema;

        private static string CreateETag(byte[] schema, ulong version)
        {
            Span<byte> hashBytes = stackalloc byte[32];
            SHA256.HashData(schema, hashBytes);
            var hash = Convert.ToBase64String(hashBytes);
            return $"\"{version}-{hash}\"";
        }

        private static string GetSchemaFileName(ISchemaDefinition schema)
            => schema.Name.Equals(ISchemaDefinition.DefaultName, StringComparison.OrdinalIgnoreCase)
                ? "schema.graphql"
                : schema.Name + ".schema.graphql";
    }

    private sealed class CachedSemanticNonNullSchemaOutput
    {
        private readonly byte[] _schema;

        public CachedSemanticNonNullSchemaOutput(ISchemaDefinition schema, ulong version, DateTimeOffset lastModifiedTime)
        {
            _schema = Encoding.UTF8.GetBytes(
                SchemaFormatter.FormatAsString(
                    schema,
                    new SchemaFormatterOptions
                    {
                        IncludeInternalDirectives = false,
                        RewriteToSemanticNonNull = true
                    }));
            FileName = GetSchemaFileName(schema);
            ETag = CreateETag(_schema, version);
            LastModified = lastModifiedTime.ToString("R");
            Version = version;
        }

        public string FileName { get; }

        public string ETag { get; }

        public ulong Version { get; }

        public string LastModified { get; }

        public ReadOnlyMemory<byte> AsMemory() => _schema;

        private static string CreateETag(byte[] schema, ulong version)
        {
            Span<byte> hashBytes = stackalloc byte[32];
            SHA256.HashData(schema, hashBytes);
            var hash = Convert.ToBase64String(hashBytes);
            return $"\"{version}-{hash}\"";
        }

        private static string GetSchemaFileName(ISchemaDefinition schema)
            => schema.Name.Equals(ISchemaDefinition.DefaultName, StringComparison.OrdinalIgnoreCase)
                ? "schema.graphql"
                : schema.Name + ".schema.graphql";
    }
}
