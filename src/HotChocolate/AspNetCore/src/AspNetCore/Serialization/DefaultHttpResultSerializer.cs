using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using static HotChocolate.AspNetCore.ErrorHelper;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.AspNetCore.Serialization;

public class DefaultHttpResultSerializer : IHttpResultSerializer
{
    private readonly IQueryResultFormatter _jsonFormatter;

    private readonly string _deferContentType;
    private readonly IResponseStreamFormatter _deferFormatter;

    private readonly string _batchContentType;
    private readonly IResponseStreamFormatter _batchFormatter;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultHttpResultSerializer" />.
    /// </summary>
    /// <param name="batchSerialization">
    /// Specifies the output-format for batched queries.
    /// </param>
    /// <param name="deferSerialization">
    /// Specifies the output-format for deferred queries.
    /// </param>
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
    public DefaultHttpResultSerializer(
        HttpResultSerialization batchSerialization = HttpResultSerialization.MultiPartChunked,
        HttpResultSerialization deferSerialization = HttpResultSerialization.MultiPartChunked,
        bool indented = false,
        JavaScriptEncoder? encoder = null)
    {
        _jsonFormatter = new JsonQueryResultFormatter(indented, encoder);
        var jsonArrayFormatter = new JsonArrayResponseStreamFormatter(_jsonFormatter);
        var multiPartFormatter = new MultiPartResponseStreamFormatter(_jsonFormatter);

        if (deferSerialization is HttpResultSerialization.JsonArray)
        {
            _deferContentType = ContentType.Json;
            _deferFormatter = jsonArrayFormatter;
        }
        else
        {
            _deferContentType = ContentType.MultiPart;
            _deferFormatter = multiPartFormatter;
        }

        if (batchSerialization is HttpResultSerialization.JsonArray)
        {
            _batchContentType = ContentType.Json;
            _batchFormatter = jsonArrayFormatter;
        }
        else
        {
            _batchContentType = ContentType.MultiPart;
            _batchFormatter = multiPartFormatter;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="DefaultHttpResultSerializer" />.
    /// </summary>
    protected DefaultHttpResultSerializer(
        IQueryResultFormatter jsonFormatter,
        HttpResultSerialization batchSerialization = HttpResultSerialization.MultiPartChunked,
        HttpResultSerialization deferSerialization = HttpResultSerialization.MultiPartChunked)
    {
        _jsonFormatter = jsonFormatter;
        var jsonArrayFormatter = new JsonArrayResponseStreamFormatter(_jsonFormatter);
        var multiPartFormatter = new MultiPartResponseStreamFormatter(_jsonFormatter);

        if (deferSerialization is HttpResultSerialization.JsonArray)
        {
            _deferContentType = ContentType.Json;
            _deferFormatter = jsonArrayFormatter;
        }
        else
        {
            _deferContentType = ContentType.MultiPart;
            _deferFormatter = multiPartFormatter;
        }

        if (batchSerialization is HttpResultSerialization.JsonArray)
        {
            _batchContentType = ContentType.Json;
            _batchFormatter = jsonArrayFormatter;
        }
        else
        {
            _batchContentType = ContentType.MultiPart;
            _batchFormatter = multiPartFormatter;
        }
    }

    public virtual string GetContentType(IExecutionResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Kind switch
        {
            SingleResult => ContentType.Json,
            DeferredResult => _deferContentType,
            BatchResult => _batchContentType,
            _ => ContentType.Json
        };
    }

    public HttpStatusCode GetStatusCode(IExecutionResult result)
    {
        return result switch
        {
            QueryResult queryResult => GetStatusCode(queryResult),
            ResponseStream streamResult => GetStatusCode(streamResult),
            _ => HttpStatusCode.InternalServerError
        };
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

    protected virtual HttpStatusCode GetStatusCode(ResponseStream responseStream)
        => HttpStatusCode.OK;

    public virtual async ValueTask SerializeAsync(
        IExecutionResult result,
        Stream stream,
        CancellationToken cancellationToken)
    {
        switch (result)
        {
            case IQueryResult queryResult:
                await _jsonFormatter.FormatAsync(queryResult, stream, cancellationToken);
                break;

            case IResponseStream { Kind: DeferredResult } streamResult:
                await _deferFormatter.FormatAsync(streamResult, stream, cancellationToken);
                break;

            case IResponseStream { Kind: BatchResult } streamResult:
                await _batchFormatter.FormatAsync(streamResult, stream, cancellationToken);
                break;

            default:
                await _jsonFormatter.FormatAsync(
                    ResponseTypeNotSupported(),
                    stream,
                    cancellationToken);
                break;
        }
    }
}
