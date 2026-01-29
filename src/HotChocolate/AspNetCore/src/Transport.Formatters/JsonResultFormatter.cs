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
        _options = options.CreateWriterOptions() with { SkipValidation = true };
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
        var jsonWriter = new JsonWriter(bufferWriter, _options);

        jsonWriter.WriteStartObject();

        if (result.RequestIndex.HasValue)
        {
            jsonWriter.WritePropertyName(RequestIndex);
            jsonWriter.WriteNumberValue(result.RequestIndex.Value);
        }

        if (result.VariableIndex.HasValue)
        {
            jsonWriter.WritePropertyName(VariableIndex);
            jsonWriter.WriteNumberValue(result.VariableIndex.Value);
        }

        WriteErrors(
            jsonWriter,
            result.Errors,
            _serializerOptions,
            default);

        if (result.Data.HasValue)
        {
            jsonWriter.WritePropertyName(Data);
            result.Data.Value.Formatter.WriteDataTo(jsonWriter);
        }

        WriteExtensions(
            jsonWriter,
            result.Extensions,
            _serializerOptions,
            default);

        if (result.IsIncremental)
        {
            WriteIncremental(
                jsonWriter,
                result,
                _serializerOptions,
                default);
        }

        jsonWriter.WriteEndObject();
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
}
