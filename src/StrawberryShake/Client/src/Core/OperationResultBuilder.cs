using System.Buffers;
using System.Text.Json;
using StrawberryShake.Json;
using static StrawberryShake.ResultFields;

namespace StrawberryShake;

/// <summary>
/// The operation result builder will use the transport response and build from it
/// the operation result.
/// </summary>
/// <typeparam name="TResultData">
/// The runtime result.
/// </typeparam>
public abstract class OperationResultBuilder<TResultData>
    : IOperationResultBuilder<JsonDocument, TResultData>
    where TResultData : class
{
    protected abstract IOperationResultDataFactory<TResultData> ResultDataFactory { get; }

    public IOperationResult<TResultData> Build(
        Response<JsonDocument> response)
    {
        TResultData? data = null;
        IOperationResultDataInfo? dataInfo = null;
        IReadOnlyList<IClientError>? errors = null;
        IReadOnlyDictionary<string, object?>? extensions = null;
        JsonElement? persistedData = null;

        try
        {
            if (response.Body is { } body)
            {
                if (body.RootElement.TryGetProperty(Data, out var dataProp)
                    && dataProp.ValueKind is JsonValueKind.Object)
                {
                    dataInfo = BuildData(dataProp);
                    data = ResultDataFactory.Create(dataInfo);

                    if (CapturePersistedData)
                    {
                        persistedData = dataProp.Clone();
                    }
                }

                if (body.RootElement.TryGetProperty(Errors, out var errorsProp)
                    && errorsProp.ValueKind is JsonValueKind.Array)
                {
                    errors = JsonErrorParser.ParseErrors(errorsProp);
                }

                if (body.RootElement.TryGetProperty(
                        ResultFields.Extensions,
                        out var extensionsProp)
                    && extensionsProp.ValueKind is JsonValueKind.Object)
                {
                    extensions = JsonExtensionParser.ParseExtensions(extensionsProp);
                }
            }
        }
        catch (Exception ex)
        {
            var list = new List<IClientError>
            {
                new ClientError(
                ex.Message,
                ErrorCodes.InvalidResultDataStructure,
                exception: ex,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(ex.StackTrace), ex.StackTrace }
                })
            };

            if (errors is not null)
            {
                list.AddRange(errors);
            }

            errors = list;
        }

        // If we have a transport error but the response does not contain any client errors
        // we will create a client error from the provided transport error.
        if (response.Exception is not null && errors is not { Count: > 0 })
        {
            errors = new IClientError[]
            {
                new ClientError(
                    response.Exception.Message,
                    ErrorCodes.InvalidResultDataStructure,
                    exception: response.Exception,
                    extensions: new Dictionary<string, object?>
                    {
                        { nameof(response.Exception.StackTrace), response.Exception.StackTrace }
                    })
            };
        }

        var contextData = response.ContextData;

        if (persistedData is { } captured)
        {
            var augmented = contextData is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(contextData);
            augmented[WellKnownContextData.PersistedData] = captured;
            contextData = augmented;
        }

        return new OperationResult<TResultData>(
            data,
            dataInfo,
            ResultDataFactory,
            errors,
            extensions,
            contextData);
    }

    /// <summary>
    /// When overridden to return <c>true</c>, the raw transport "data" payload is captured
    /// into <see cref="IOperationResult.ContextData"/> under
    /// <see cref="WellKnownContextData.PersistedData"/> so it can be persisted and later
    /// rehydrated via <see cref="BuildFromPersistedData"/>.
    /// </summary>
    protected virtual bool CapturePersistedData => false;

    /// <summary>
    /// Builds a runtime operation result from a previously persisted transport "data"
    /// payload, without executing the operation.
    /// </summary>
    /// <param name="persistedData">
    /// The UTF-8 encoded JSON of the GraphQL response "data" object.
    /// </param>
    /// <returns>
    /// Returns the runtime result.
    /// </returns>
    public IOperationResult<TResultData> BuildFromPersistedData(ReadOnlyMemory<byte> persistedData)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(Data);
            writer.WriteRawValue(persistedData.Span, skipInputValidation: true);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(bufferWriter.WrittenMemory);
        return Build(new Response<JsonDocument>(document, null));
    }

    protected abstract IOperationResultDataInfo BuildData(JsonElement obj);
}
