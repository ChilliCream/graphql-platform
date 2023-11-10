using System;
using System.IO;
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
    private static readonly byte[] _newLine = "\n"u8.ToArray();

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
            await WriteNextMessageAsync((IQueryResult)result, outputStream, ct).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
            await WriteCompleteMessage(outputStream, ct).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            var responseStream = (IResponseStream)result;

            // synchronization of the output stream is required to ensure that the messages are not
            // interleaved.
            using var synchronization = new SemaphoreSlim(1, 1);

            // we need to keep track if the stream is completed so that we can stop sending keep
            // alive messages.
            var completion = new TaskCompletionSource<bool>();

            // we await all tasks so that we can catch all exceptions.
            await Task.WhenAll(
                ProcessResponseStreamAsync(synchronization, completion, responseStream, outputStream, ct),
                SendKeepAliveMessagesAsync(synchronization, completion, outputStream, ct));
        }

        else
        {
            throw new NotSupportedException();
        }
    }

    private static async Task SendKeepAliveMessagesAsync(
        SemaphoreSlim synchronization,
        TaskCompletionSource<bool> completion,
        Stream outputStream,
        CancellationToken ct)
    {
        while (true)
        {
            await Task.WhenAny(Task.Delay(_keepAliveTimeSpan, ct), completion.Task);

            if (!ct.IsCancellationRequested && !completion.Task.IsCompleted)
            {
                // we do not need try-finally here because we dispose the semaphore in the parent
                // method.
                await synchronization.WaitAsync(ct);

                await WriteKeepAliveAndFlush(outputStream, ct);

                synchronization.Release();
            }
            else
            {
                break;
            }
        }
    }

    private async Task ProcessResponseStreamAsync(
        SemaphoreSlim synchronization,
        TaskCompletionSource<bool> completion,
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        await foreach (var queryResult in responseStream.ReadResultsAsync().WithCancellation(ct).ConfigureAwait(false))
        {
            // we do not need try-finally here because we dispose the semaphore in the parent
            // method.

            await synchronization.WaitAsync(ct);

            try
            {
                await WriteNextMessageAsync(queryResult, outputStream, ct).ConfigureAwait(false);
            }
            finally
            {
                await queryResult.DisposeAsync().ConfigureAwait(false);
            }

            await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);

            synchronization.Release();
        }

        await synchronization.WaitAsync(ct);

        await WriteCompleteMessage(outputStream, ct).ConfigureAwait(false);
        await WriteNewLineAndFlushAsync(outputStream, ct).ConfigureAwait(false);

        synchronization.Release();
        completion.SetResult(true);
    }

    private async ValueTask WriteNextMessageAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken ct)
    {
#if NET6_0_OR_GREATER
        await outputStream.WriteAsync(_eventField, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_nextEvent, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, ct).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_eventField, 0, _eventField.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_nextEvent, 0, _nextEvent.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length, ct).ConfigureAwait(false);
#endif

        using var bufferWriter = new ArrayWriter();
        FormatPayload(bufferWriter, result);

#if NET6_0_OR_GREATER
        await outputStream.WriteAsync(_dataField, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(bufferWriter.GetWrittenMemory(), ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, ct).ConfigureAwait(false);
#else
        var buffer = bufferWriter.GetInternalBuffer();
        await outputStream.WriteAsync(_dataField, 0, _dataField.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(buffer, 0, bufferWriter.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length, ct).ConfigureAwait(false);
#endif
    }

    private void FormatPayload(ArrayWriter bufferWriter, IQueryResult result)
    {
        using var writer = new Utf8JsonWriter(bufferWriter, _options);
        _payloadFormatter.Format(result, writer);
    }

    private static async ValueTask WriteCompleteMessage(Stream outputStream, CancellationToken ct)
    {
#if NET6_0_OR_GREATER
        await outputStream.WriteAsync(_eventField, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_completeEvent, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, ct).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_eventField, 0, _eventField.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_completeEvent, 0, _completeEvent.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(_newLine, 0, _newLine.Length, ct).ConfigureAwait(false);
#endif
    }

    private static async ValueTask WriteNewLineAndFlushAsync(
        Stream outputStream,
        CancellationToken ct)
    {
#if NET6_0_OR_GREATER
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
#if NET6_0_OR_GREATER
        await outputStream.WriteAsync(_keepAlive, ct).ConfigureAwait(false);
#else
        await outputStream.WriteAsync(_keepAlive, 0, _keepAlive.Length, ct).ConfigureAwait(false);
#endif
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }
}
