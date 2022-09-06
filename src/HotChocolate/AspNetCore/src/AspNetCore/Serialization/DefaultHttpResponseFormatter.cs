using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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

    public async ValueTask FormatAsync(
        IExecutionResult result,
        StringValues acceptHeaderValue,
        HttpStatusCode? statusCode,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (!TryGetFormatter(result, acceptHeaderValue, out var value))
        {
            // todo: write error response
            return;
        }

        var contentType = value.ContentType;
        var formatter = value.Formatter;

        if (result.Kind is SingleResult)
        {
            statusCode ??= GetStatusCode((IQueryResult)result);

            response.ContentType = contentType;
            response.StatusCode = (int)statusCode;

            await formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            statusCode ??= GetStatusCode((IResponseStream)result);

            response.ContentType = contentType;
            response.StatusCode = (int)statusCode;

            await formatter.FormatAsync(result, response.Body, cancellationToken);
        }
        else
        {
            // TODO : Throw helper.
            throw new NotSupportedException("The execution result kind is not supported.");
        }
    }

    protected virtual HttpStatusCode GetStatusCode(IQueryResult result)
    {
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

    protected virtual HttpStatusCode GetStatusCode(IResponseStream responseStream)
        => HttpStatusCode.OK;

    private bool TryGetFormatter(
        IExecutionResult result,
        StringValues acceptHeaderValue,
        out (string ContentType, IExecutionResultFormatter Formatter) value)
    {
        var count = acceptHeaderValue.Count;

        if (count == 0)
        {
            if (result.Kind is SingleResult)
            {
                value = (ContentType.Json, _jsonFormatter);
                return true;
            }

            if (result.Kind is DeferredResult or BatchResult)
            {
                value = (ContentType.Json, _multiPartFormatter);
                return true;
            }

            if (result.Kind is SubscriptionResult)
            {
                value = (ContentType.Json, _eventStreamFormatter);
            }

            value = default;
            return false;
        }

        var needsStream = result.Kind is not SingleResult;
        var needsSubscription = result.Kind is SubscriptionResult;

        if (count == 1)
        {
            var contentType = acceptHeaderValue[0].AsSpan();

            if (!needsStream && contentType.StartsWith(ContentType.GraphQLResponseSpan()))
            {
                value = (ContentType.GraphQLResponse, _jsonFormatter);
                return true;
            }

            if (!needsStream && contentType.StartsWith(ContentType.JsonSpan()))
            {
                value = (ContentType.Json, _jsonFormatter);
                return true;
            }

            if (!needsSubscription && contentType.StartsWith(ContentType.MultiPartMixedSpan()))
            {
                value = (ContentType.MultiPartMixed, _multiPartFormatter);
                return true;
            }

            if (contentType.StartsWith(ContentType.EventStreamSpan()))
            {
                value = (ContentType.EventStream, _eventStreamFormatter);
                return true;
            }

            value = default;
            return false;
        }

        string[] innerArray = acceptHeaderValue;
        ref var searchSpace = ref MemoryMarshal.GetReference(innerArray.AsSpan());

        for (var i = 0; i < innerArray.Length; i++)
        {
            var contentType = Unsafe.Add(ref searchSpace, i).AsSpan();

            if (!needsStream && contentType.StartsWith(ContentType.GraphQLResponseSpan()))
            {
                value = (ContentType.GraphQLResponse, _jsonFormatter);
                return true;
            }

            if (!needsStream && contentType.StartsWith(ContentType.JsonSpan()))
            {
                value = (ContentType.Json, _jsonFormatter);
                return true;
            }

            if (!needsSubscription && contentType.StartsWith(ContentType.MultiPartMixedSpan()))
            {
                value = (ContentType.MultiPartMixed, _multiPartFormatter);
                return true;
            }

            if (contentType.StartsWith(ContentType.EventStreamSpan()))
            {
                value = (ContentType.EventStream, _eventStreamFormatter);
                return true;
            }
        }

        value = default;
        return false;
    }

}
