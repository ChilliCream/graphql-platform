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
    private static readonly TimeSpan _keepAliveTimeSpan = TimeSpan.FromSeconds(12);

    private static readonly byte[] _eventField = "event: "u8.ToArray();
    private static readonly byte[] _dataField = "data: "u8.ToArray();
    private static readonly byte[] _nextEvent = "next"u8.ToArray();
    private static readonly byte[] _keepAlive = ":\n\n"u8.ToArray();
    private static readonly byte[] _completeEvent = "complete"u8.ToArray();
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

            // we first open a async enumerator over the response stream.
            var enumerator = responseStream.ReadResultsAsync().GetAsyncEnumerator(ct);

            // we then move to the first result.
            var moveNextTask = enumerator.MoveNextAsync().AsTask();

            while (!ct.IsCancellationRequested)
            {
                // we wait for the next result or the keep alive timeout.
                await Task.WhenAny(moveNextTask, Task.Delay(_keepAliveTimeSpan, ct))
                    .ConfigureAwait(false);

                // if the moveNextTask is completed then we received a result. If it is not
                // completed then we arrived at the keep alive timeout.
                if (moveNextTask.IsCompleted)
                {
                    // in case we received a result we await the task check if the stream is
                    // completed
                    if (await moveNextTask)
                    {
                        var queryResult = enumerator.Current;
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

                        // we then move to the next result.
                        moveNextTask = enumerator.MoveNextAsync().AsTask();
                    }
                    else
                    {
                        await WriteCompleteMessage(outputStream).ConfigureAwait(false);
                        await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);

                        return;
                    }
                }
                else
                {
                    // if we arrived at the keep alive timeout we write a keep alive message.
                    await WriteKeepAliveAndFlush(outputStream, ct).ConfigureAwait(false);
                }
            }
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
            var buffer = bufferWriter.GetWrittenMemory().Slice(read);
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

    private static async ValueTask WriteKeepAliveAndFlush(
        Stream outputStream,
        CancellationToken ct)
    {
#if NETCOREAPP3_1_OR_GREATER
        await outputStream.WriteAsync(_keepAlive, ct).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_keepAlive, 0, _keepAlive.Length, ct).ConfigureAwait(false);
#endif
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }
}
