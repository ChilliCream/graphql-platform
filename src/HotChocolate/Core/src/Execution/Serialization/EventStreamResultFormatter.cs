using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default GraphQL-SSE formatter for <see cref="IExecutionResult"/>.
/// https://github.com/enisdenjo/graphql-sse/blob/master/PROTOCOL.md
/// </summary>
public sealed class EventStreamResultFormatter : IExecutionResultFormatter
{
    private static readonly byte[] _eventField
        = { (byte)'e', (byte)'v', (byte)'e', (byte)'n', (byte)'t', (byte)':', (byte)' ' };
    private static readonly byte[] _dataField
        = { (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':', (byte)' ' };
    private static readonly byte[] _nextEvent
        = { (byte)'n', (byte)'e', (byte)'x', (byte)'t' };
    private static readonly byte[] _completeEvent
        =
        {
            (byte)'c', (byte)'o', (byte)'m', (byte)'p',
            (byte)'l', (byte)'e', (byte)'t', (byte)'e'
        };
    private static readonly byte[] _newLine = { (byte)'\n' };

    private readonly JsonResultFormatter _payloadFormatter;
    private readonly JsonWriterOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="EventStreamResultFormatter" />.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public EventStreamResultFormatter(JsonResultFormatterOptions options)
    {
        _options = options.CreateWriterOptions();
        _payloadFormatter = new JsonResultFormatter(options);
    }

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync(IExecutionResult, Stream, CancellationToken)" />
    public ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream is null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        return FormatInternalAsync(result, outputStream, cancellationToken);
    }

    private async ValueTask FormatInternalAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken ct)
    {
        if (result.Kind is SingleResult)
        {
            await WriteNextMessageAsync((IQueryResult)result, outputStream).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
            await WriteCompleteMessage(outputStream).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            var responseStream = (IResponseStream)result;

            await foreach (var queryResult in responseStream.ReadResultsAsync()
                .WithCancellation(ct).ConfigureAwait(false))
            {
                try
                {
                    await WriteNextMessageAsync(queryResult, outputStream)
                        .ConfigureAwait(false);
                }
                finally
                {
                    await queryResult.DisposeAsync().ConfigureAwait(false);
                }

                await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
            }

            await WriteCompleteMessage(outputStream).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private async ValueTask WriteNextMessageAsync(IQueryResult result, Stream outputStream)
    {
#if NETCOREAPP3_1_OR_GREATER
        await outputStream.WriteAsync(_eventField).ConfigureAwait(false);
        await outputStream.WriteAsync(_nextEvent).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_eventField, 0, _eventField.Length).ConfigureAwait(false);
        await outputStream.WriteAsync(_nextEvent, 0, _nextEvent.Length).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length).ConfigureAwait(false);
#endif

        using var bufferWriter = new ArrayWriter();
        await using (var writer = new Utf8JsonWriter(bufferWriter, _options))
        {
            _payloadFormatter.Format(result, writer);
        }

        var read = 0;
        while (read < bufferWriter.Length)
        {
            var buffer = bufferWriter.Body.Slice(read);
            if (buffer.Span.IndexOf(_newLine) is var newLineIndex && newLineIndex != -1)
            {
                buffer = buffer.Slice(0, newLineIndex);
            }

#if NETCOREAPP3_1_OR_GREATER
            await outputStream.WriteAsync(_dataField).ConfigureAwait(false);
            await outputStream.WriteAsync(buffer).ConfigureAwait(false);
            await outputStream.WriteAsync(_newLine).ConfigureAwait(false);
#else
            await outputStream.WriteAsync(_dataField, 0, _dataField.Length).ConfigureAwait(false);
            await outputStream.WriteAsync(bufferWriter.GetInternalBuffer(), read, buffer.Length)
                .ConfigureAwait(false);
            await outputStream.WriteAsync(_newLine, 0, _newLine.Length).ConfigureAwait(false);
#endif

            read += buffer.Length + 1;
        }
    }

    private static async ValueTask WriteCompleteMessage(Stream outputStream)
    {
#if NETCOREAPP3_1_OR_GREATER
        await outputStream.WriteAsync(_eventField).ConfigureAwait(false);
        await outputStream.WriteAsync(_completeEvent).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_eventField, 0, _eventField.Length).ConfigureAwait(false);
        await outputStream.WriteAsync(_completeEvent, 0, _completeEvent.Length).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length).ConfigureAwait(false);
#endif
    }

    private static async ValueTask WriteNewLineAndFlushAsync(
        Stream outputStream,
        CancellationToken ct)
    {
#if NETCOREAPP3_1_OR_GREATER
        await outputStream.WriteAsync(_newLine, ct).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length, ct).ConfigureAwait(false);
#endif
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }
}
