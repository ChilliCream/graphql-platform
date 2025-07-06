using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using static HotChocolate.Execution.ResultFieldNames;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.Transport.Formatters;

/// <summary>
/// The default JSON formatter for <see cref="IOperationResult"/>.
/// </summary>
public sealed class JsonResultFormatter : IOperationResultFormatter, IExecutionResultFormatter
{
    private readonly JsonWriterOptions _options;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly JsonNullIgnoreCondition _nullIgnoreCondition;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonResultFormatter"/> with default options.
    /// </summary>
    /// <param name="indented">
    /// Defines if the JSON should be formatted with indentations.
    /// </param>
    public JsonResultFormatter(bool indented = false)
        : this(new JsonResultFormatterOptions { Indented = indented })
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="JsonResultFormatter"/>.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public JsonResultFormatter(JsonResultFormatterOptions options)
    {
        _options = options.CreateWriterOptions();
        _serializerOptions = options.CreateSerializerOptions();
        _nullIgnoreCondition = options.NullIgnoreCondition;
    }

    /// <summary>
    /// The default JSON formatter for <see cref="IOperationResult"/> with indentations.
    /// </summary>
    public static JsonResultFormatter Indented { get; } = new(true);

    /// <summary>
    /// The default JSON formatter for <see cref="IOperationResult"/> without indentations.
    /// </summary>
    public static JsonResultFormatter Default { get; } = new();

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync"/>
    public async ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        switch (result)
        {
            case IOperationResult singleResult:
                await FormatInternalAsync(singleResult, outputStream, cancellationToken).ConfigureAwait(false);
                break;

            case OperationResultBatch resultBatch:
                await FormatInternalAsync(resultBatch, outputStream, cancellationToken).ConfigureAwait(false);
                break;

            case IResponseStream responseStream:
                await FormatInternalAsync(responseStream, outputStream, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException(
                    $"The result type '{result.GetType().FullName}' is not supported by the JSON formatter.");
        }
    }

    /// <summary>
    /// Formats a query result as JSON string.
    /// </summary>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void Format(IOperationResult result, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        WriteResult(writer, result);
    }

    /// <summary>
    /// Formats a <see cref="IError"/> as JSON string.
    /// </summary>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatError(IError error, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(writer);

        WriteError(writer, error, _serializerOptions, _nullIgnoreCondition);
    }

    /// <summary>
    /// Formats a list of <see cref="IError"/>s as JSON array string.
    /// </summary>
    /// <param name="errors">
    /// The list of error objects.
    /// </param>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// <paramref name="writer"/> is <c>null</c>.
    /// </exception>
    public void FormatErrors(IReadOnlyList<IError> errors, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            WriteError(writer, errors[i], _serializerOptions, _nullIgnoreCondition);
        }

        writer.WriteEndArray();
    }

    public void FormatDictionary(IReadOnlyDictionary<string, object?> dictionary, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(writer);

        WriteDictionary(writer, dictionary, _serializerOptions, _nullIgnoreCondition);
    }

    public void Format(IOperationResult result, IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        FormatInternal(result, writer);
    }

    private void FormatInternal(IOperationResult result, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, _options);
        WriteResult(jsonWriter, result);
        jsonWriter.Flush();
    }

    public ValueTask FormatAsync(
        IOperationResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(outputStream);

        return FormatInternalAsync(result, outputStream, cancellationToken);
    }

    private async ValueTask FormatInternalAsync(
        IOperationResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArrayWriter();
        FormatInternal(result, buffer);

        await outputStream
            .WriteAsync(buffer.WrittenMemory, cancellationToken)
            .ConfigureAwait(false);

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FormatInternalAsync(
        OperationResultBatch resultBatch,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArrayWriter();

        foreach (var result in resultBatch.Results)
        {
            switch (result)
            {
                case IOperationResult singleResult:
                    FormatInternal(singleResult, buffer);
                    break;

                case IResponseStream batchResult:
                {
                    await foreach (var partialResult in batchResult.ReadResultsAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        try
                        {
                            FormatInternal(partialResult, buffer);
                        }
                        finally
                        {
                            await partialResult.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    break;
                }
            }
        }

        await outputStream
            .WriteAsync(buffer.WrittenMemory, cancellationToken)
            .ConfigureAwait(false);

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FormatInternalAsync(
        IResponseStream batchResult,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArrayWriter();

        await foreach (var partialResult in batchResult.ReadResultsAsync()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            try
            {
                FormatInternal(partialResult, buffer);
            }
            finally
            {
                await partialResult.DisposeAsync().ConfigureAwait(false);
            }
        }

        await outputStream
            .WriteAsync(buffer.WrittenMemory, cancellationToken)
            .ConfigureAwait(false);

        await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResult(Utf8JsonWriter writer, IOperationResult result)
    {
        writer.WriteStartObject();

        if (result.RequestIndex.HasValue)
        {
            writer.WriteNumber(RequestIndex, result.RequestIndex.Value);
        }

        if (result.VariableIndex.HasValue)
        {
            writer.WriteNumber(VariableIndex, result.VariableIndex.Value);
        }

        WriteErrors(writer, result.Errors);
        WriteData(writer, result);
        WriteItems(writer, result.Items);
        WriteIncremental(writer, result.Incremental);
        WriteExtensions(writer, result.Extensions, _serializerOptions, _nullIgnoreCondition);
        WritePatchInfo(writer, result);
        WriteHasNext(writer, result);

        writer.WriteEndObject();
    }

    private static void WritePatchInfo(
        Utf8JsonWriter writer,
        IOperationResult result)
    {
        if (result.Label is not null)
        {
            writer.WriteString("label", result.Label);
        }

        if (result.Path is not null)
        {
            WritePath(writer, result.Path);
        }
    }

    private static void WriteHasNext(
        Utf8JsonWriter writer,
        IOperationResult result)
    {
        if (result.HasNext.HasValue)
        {
            writer.WriteBoolean("hasNext", result.HasNext.Value);
        }
    }

    private void WriteData(
        Utf8JsonWriter writer,
        IOperationResult result)
    {
        if (!result.IsDataSet)
        {
            return;
        }

        if (result.Data is null)
        {
            writer.WriteNull(Data);
            return;
        }

        writer.WritePropertyName(Data);

        WriteValue(writer, result.Data, _serializerOptions, _nullIgnoreCondition);
    }

    private void WriteItems(Utf8JsonWriter writer, IReadOnlyList<object?>? items)
    {
        if (items is { Count: > 0 })
        {
            writer.WritePropertyName(Items);

            writer.WriteStartArray();

            for (var i = 0; i < items.Count; i++)
            {
                WriteValue(writer, items[i], _serializerOptions, _nullIgnoreCondition);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteErrors(Utf8JsonWriter writer, IReadOnlyList<IError>? errors)
    {
        if (errors is { Count: > 0 })
        {
            writer.WritePropertyName(Errors);

            writer.WriteStartArray();

            for (var i = 0; i < errors.Count; i++)
            {
                WriteError(writer, errors[i], _serializerOptions, _nullIgnoreCondition);
            }

            writer.WriteEndArray();
        }
    }

    private void WriteIncremental(Utf8JsonWriter writer, IReadOnlyList<IOperationResult>? patches)
    {
        if (patches is { Count: > 0 })
        {
            writer.WritePropertyName(Incremental);

            writer.WriteStartArray();

            for (var i = 0; i < patches.Count; i++)
            {
                WriteResult(writer, patches[i]);
            }

            writer.WriteEndArray();
        }
    }
}
