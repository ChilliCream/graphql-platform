using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using Microsoft.AspNetCore.Http;
using static HotChocolate.AspNetCore.AcceptMediaTypeKind;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.AspNetCore.Serialization;

public class DefaultHttpResponseFormatter : IHttpResponseFormatter
{
    private readonly JsonQueryResultFormatter _jsonFormatter;
    private readonly MultiPartResponseStreamFormatter _multiPartFormatter;

    // TODO : implement this one!
    private readonly IExecutionResultFormatter _eventStreamFormatter = default!;

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
    {
        _jsonFormatter = new JsonQueryResultFormatter(indented, encoder);
        _multiPartFormatter = new MultiPartResponseStreamFormatter(_jsonFormatter);
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
        HttpStatusCode? statusCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetFormatter(result, acceptMediaTypes, out var format))
        {
            // todo: write error response
            return;
        }

        if (result.Kind is SingleResult)
        {
            statusCode ??= GetStatusCode((IQueryResult)result, format);

            response.ContentType = format.ContentType;
            response.StatusCode = (int)statusCode;

            await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            statusCode ??= GetStatusCode((IResponseStream)result, format);

            response.ContentType = format.ContentType;
            response.StatusCode = (int)statusCode;

            await format.Formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else
        {
            // TODO : Throw helper.
            throw new NotSupportedException("The execution result kind is not supported.");
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

        if (format.Kind is ResponseContentType.GraphQLResponse)
        {
            if (proposedStatusCode.HasValue)
            {
                return proposedStatusCode.Value;
            }

            if (result.Data is not null)
            {
                return HttpStatusCode.OK;
            }

            if (result.ContextData is not null)
            {
                if (result.ContextData.ContainsKey(WellKnownContextData.ValidationErrors))
                {
                    return HttpStatusCode.BadRequest;
                }

                if (result.ContextData.ContainsKey(WellKnownContextData.OperationNotAllowed))
                {
                    return HttpStatusCode.MethodNotAllowed;
                }
            }

            // if a persisted query is not found when using the active persisted query pipeline
            // is used we will return a success status code.
            if (result.Errors is { Count: 1 } &&
                result.Errors[0] is { Code: ErrorCodes.Execution.PersistedQueryNotFound })
            {
                return HttpStatusCode.OK;
            }

            return HttpStatusCode.InternalServerError;
        }

        // we allow for users to implement alternative protocols or response content-type.
        // if we end up here the user did not fully implement all necessary parts to add support
        // for an alternative protocols or response content-type.
        // TODO : throw helper.
        throw new NotSupportedException(
            $"The specified response content-type `{format.ContentType}` is not supported.");
    }

    protected virtual HttpStatusCode GetStatusCode(
        IResponseStream responseStream,
        FormatInfo format)
        => HttpStatusCode.OK;

    private bool TryGetFormatter(
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        out FormatInfo formatInfo)
    {
        if (acceptMediaTypes.Length == 0)
        {
            if (result.Kind is SingleResult)
            {
                formatInfo = new FormatInfo(
                    ContentType.Json,
                    ResponseContentType.Json,
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
                    _eventStreamFormatter);
                return true;
            }

            formatInfo = default;
            return false;
        }

        var needsStream = result.Kind is not SingleResult;
        var needsSubscription = result.Kind is SubscriptionResult;

        if (acceptMediaTypes.Length == 1)
        {
            var mediaType = acceptMediaTypes[0];

            if (!needsStream && mediaType.Kind is ApplicationGraphQL or AllApplication or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.GraphQLResponse,
                    ResponseContentType.GraphQLResponse,
                    _jsonFormatter);
                return true;
            }

            if (!needsStream && mediaType.Kind is ApplicationJson)
            {
                formatInfo = new FormatInfo(
                    ContentType.Json,
                    ResponseContentType.Json,
                    _jsonFormatter);
                return true;
            }

            if (!needsSubscription && mediaType.Kind is MultiPartMixed or AllMultiPart or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.MultiPartMixed,
                    ResponseContentType.MultiPartMixed,
                    _multiPartFormatter);
                return true;
            }

            if (mediaType.Kind is EventStream or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.EventStream,
                    ResponseContentType.EventStream,
                    _eventStreamFormatter);
                return true;
            }

            formatInfo = default;
            return false;
        }

        ref var searchSpace = ref MemoryMarshal.GetReference(acceptMediaTypes.AsSpan());

        for (var i = 0; i < acceptMediaTypes.Length; i++)
        {
            var mediaType = Unsafe.Add(ref searchSpace, i);

            if (!needsStream && mediaType.Kind is ApplicationGraphQL or AllApplication or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.GraphQLResponse,
                    ResponseContentType.GraphQLResponse,
                    _jsonFormatter);
                return true;
            }

            if (!needsStream && mediaType.Kind is ApplicationJson)
            {
                formatInfo = new FormatInfo(
                    ContentType.Json,
                    ResponseContentType.Json,
                    _jsonFormatter);
                return true;
            }

            if (!needsSubscription && mediaType.Kind is MultiPartMixed or AllMultiPart or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.MultiPartMixed,
                    ResponseContentType.MultiPartMixed,
                    _multiPartFormatter);
                return true;
            }

            if (mediaType.Kind is EventStream or All)
            {
                formatInfo = new FormatInfo(
                    ContentType.EventStream,
                    ResponseContentType.EventStream,
                    _eventStreamFormatter);
                return true;
            }
        }

        formatInfo = default;
        return false;
    }

    protected readonly struct FormatInfo
    {
        public FormatInfo(
            string contentType,
            ResponseContentType kind,
            IExecutionResultFormatter formatter)
        {
            ContentType = contentType;
            Kind = kind;
            Formatter = formatter;
        }

        public string ContentType { get; }

        public ResponseContentType Kind { get; }

        public IExecutionResultFormatter Formatter { get; }
    }
}
