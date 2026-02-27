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
        ExecutionResultFormatFlags flags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var useIncrementalRfc1 =
            (flags & ExecutionResultFormatFlags.IncrementalRfc1)
                == ExecutionResultFormatFlags.IncrementalRfc1;

        return result switch
        {
            OperationResult singleResult => FormatInternalAsync(
                singleResult,
                writer,
                useIncrementalRfc1,
                cancellationToken),
            OperationResultBatch resultBatch => FormatInternalAsync(
                resultBatch,
                writer,
                useIncrementalRfc1,
                cancellationToken),
            IResponseStream responseStream => FormatInternalAsync(
                responseStream,
                writer,
                useIncrementalRfc1,
                cancellationToken),
            _ => throw new NotSupportedException($"The result type '{result.GetType().FullName}' is not supported.")
        };
    }

    public void Format(OperationResult result, IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        OperationResultFormatterContext? context = null;
        FormatInternal(result, writer, useIncrementalRfc1: false, ref context);
    }

    internal void Format(
        OperationResult result,
        IBufferWriter<byte> writer,
        bool useIncrementalRfc1,
        ref OperationResultFormatterContext? context)
    {
        FormatInternal(result, writer, useIncrementalRfc1, ref context);
    }

    public ValueTask FormatAsync(
        OperationResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        OperationResultFormatterContext? context = null;
        return FormatInternalAsync(result, writer, useIncrementalRfc1: false, ref context, cancellationToken);
    }

    internal ValueTask FormatAsync(
        OperationResult result,
        PipeWriter writer,
        bool useIncrementalRfc1,
        ref OperationResultFormatterContext? context,
        CancellationToken cancellationToken = default)
        => FormatInternalAsync(result, writer, useIncrementalRfc1, ref context, cancellationToken);

    private void FormatInternal(
        OperationResult result,
        IBufferWriter<byte> bufferWriter,
        bool useIncrementalRfc1,
        ref OperationResultFormatterContext? context)
    {
        var jsonWriter = new JsonWriter(bufferWriter, _options, _nullIgnoreCondition);
        Format(result, jsonWriter, useIncrementalRfc1, ref context);
    }

    public void Format(OperationResult result, JsonWriter writer)
    {
        OperationResultFormatterContext? context = null;
        Format(result, writer, useIncrementalRfc1: false, ref context);
    }

    private void Format(
        OperationResult result,
        JsonWriter writer,
        bool useIncrementalRfc1,
        ref OperationResultFormatterContext? context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

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

        WriteErrors(
            writer,
            result.Errors,
            _serializerOptions);

        if (result.Data.HasValue)
        {
            writer.WritePropertyName(Data);
            result.Data.Value.Formatter.WriteDataTo(writer);
        }

        WriteExtensions(
            writer,
            result.Extensions,
            _serializerOptions);

        if (result.IsIncremental)
        {
            if (useIncrementalRfc1)
            {
                context ??= new OperationResultFormatterContext();
                IncrementalRfc1ResultFormatAdapter.WriteIncremental(writer, result, _serializerOptions, context);
            }
            else
            {
                WriteIncremental(writer, result, _serializerOptions);
            }
        }

        writer.WriteEndObject();
    }

    private ValueTask FormatInternalAsync(
        OperationResult result,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken cancellationToken)
    {
        OperationResultFormatterContext? context = null;
        return FormatInternalAsync(
            result,
            writer,
            useIncrementalRfc1,
            ref context,
            cancellationToken);
    }

    private ValueTask FormatInternalAsync(
        OperationResult result,
        PipeWriter writer,
        bool useIncrementalRfc1,
        ref OperationResultFormatterContext? context,
        CancellationToken cancellationToken)
    {
        FormatInternal(result, writer, useIncrementalRfc1, ref context);
        return FlushAsync(writer, cancellationToken);

        static async ValueTask FlushAsync(PipeWriter w, CancellationToken ct)
            => await w.FlushAsync(ct).ConfigureAwait(false);
    }

    private async ValueTask FormatInternalAsync(
        OperationResultBatch resultBatch,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken cancellationToken = default)
    {
        foreach (var result in resultBatch.Results)
        {
            switch (result)
            {
                case OperationResult singleResult:
                    OperationResultFormatterContext? singleContext = null;
                    FormatInternal(singleResult, writer, useIncrementalRfc1, ref singleContext);
                    break;

                case IResponseStream batchResult:
                    OperationResultFormatterContext? streamContext = null;

                    await foreach (var partialResult in batchResult.ReadResultsAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        try
                        {
                            FormatInternal(partialResult, writer, useIncrementalRfc1, ref streamContext);
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
        IResponseStream responseStream,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken cancellationToken = default)
    {
        OperationResultFormatterContext? context = null;

        await foreach (var partialResult in responseStream.ReadResultsAsync()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            try
            {
                FormatInternal(partialResult, writer, useIncrementalRfc1, ref context);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await partialResult.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
