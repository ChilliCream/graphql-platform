using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.JsonValueFormatter;
using static HotChocolate.Execution.ResultFieldNames;

namespace HotChocolate.Transport.Formatters;

/// <summary>
/// The default JSON formatter for <see cref="OperationResult"/>.
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
    /// The default JSON formatter for <see cref="OperationResult"/> with indentations.
    /// </summary>
    public static JsonResultFormatter Indented { get; } = new(true);

    /// <summary>
    /// The default JSON formatter for <see cref="OperationResult"/> without indentations.
    /// </summary>
    public static JsonResultFormatter Default { get; } = new();

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync"/>
    public ValueTask FormatAsync(
        IExecutionResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        return result switch
        {
            OperationResult singleResult => FormatInternalAsync(singleResult, writer, cancellationToken),
            OperationResultBatch resultBatch => FormatInternalAsync(resultBatch, writer, cancellationToken),
            IResponseStream responseStream => FormatInternalAsync(responseStream, writer, cancellationToken),
            _ => throw new NotSupportedException($"The result type '{result.GetType().FullName}' is not supported.")
        };
    }

    public void Format(OperationResult result, IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        FormatInternal(result, writer);
    }

    public ValueTask FormatAsync(
        OperationResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        return FormatInternalAsync(result, writer, cancellationToken);
    }

    private void FormatInternal(OperationResult result, IBufferWriter<byte> bufferWriter)
    {
        if (result.JsonFormatter is { } formatter)
        {
            formatter.WriteTo(result, bufferWriter, _options);
            return;
        }

        var writer = new JsonWriter(bufferWriter, _options);
        WriteResult(writer, result);
    }

    private async ValueTask FormatInternalAsync(
        OperationResult result,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        FormatInternal(result, writer);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FormatInternalAsync(
        OperationResultBatch resultBatch,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        foreach (var result in resultBatch.Results)
        {
            switch (result)
            {
                case OperationResult singleResult:
                    FormatInternal(singleResult, writer);
                    break;

                case IResponseStream batchResult:
                    await foreach (var partialResult in batchResult.ReadResultsAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        try
                        {
                            FormatInternal(partialResult, writer);
                            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            await partialResult.DisposeAsync().ConfigureAwait(false);
                        }
                    }

                    break;
            }
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FormatInternalAsync(
        IResponseStream batchResult,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        await foreach (var partialResult in batchResult.ReadResultsAsync()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            try
            {
                FormatInternal(partialResult, writer);
            }
            finally
            {
                await partialResult.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private void WriteResult(JsonWriter writer, OperationResult result)
    {
        writer.WriteStartObject();

        if (result.RequestIndex.HasValue)
        {
            writer.WritePropertyName(RequestIndex);
            writer.WriteNumberValue(result.RequestIndex.Value);
        }

        if (result.VariableIndex.HasValue)
        {
            writer.WritePropertyName(VariableIndex);
            writer.WriteNumberValue(result.VariableIndex.Value);
        }

        WriteErrors(writer, result.Errors);
        WriteData(writer, result);
        WriteExtensions(writer, result.Extensions, _serializerOptions, _nullIgnoreCondition);
        WriteHasNext(writer, result);

        writer.WriteEndObject();
    }

    private static void WriteHasNext(JsonWriter writer, OperationResult result)
    {
        if (result.HasNext.HasValue)
        {
            writer.WritePropertyName("hasNext"u8);
            writer.WriteBooleanValue(result.HasNext.Value);
        }
    }

    private void WriteData(JsonWriter writer, OperationResult result)
    {
        if (!result.IsDataSet)
        {
            return;
        }

        if (result.Data is null)
        {
            writer.WritePropertyName(Data);
            writer.WriteNullValue();
            return;
        }

        writer.WritePropertyName(Data);
        WriteValue(writer, result.Data, _serializerOptions, _nullIgnoreCondition);
    }

    private void WriteErrors(JsonWriter writer, IReadOnlyList<IError>? errors)
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
}
