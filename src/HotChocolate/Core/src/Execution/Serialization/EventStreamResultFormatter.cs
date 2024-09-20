using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Serialization.EventStreamResultFormatterEventSource;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default GraphQL-SSE formatter for <see cref="IExecutionResult"/>.
/// https://github.com/enisdenjo/graphql-sse/blob/master/PROTOCOL.md
/// </summary>
public sealed class EventStreamResultFormatter : IExecutionResultFormatter
{
    private readonly JsonResultFormatter _payloadFormatter;

    /// <summary>
    /// Initializes a new instance of <see cref="EventStreamResultFormatter"/>.
    /// </summary>
    /// <param name="options">
    /// The options to configure the JSON writer.
    /// </param>
    public EventStreamResultFormatter(JsonResultFormatterOptions options)
    {
        _payloadFormatter = new JsonResultFormatter(options);
    }

    /// <summary>
    /// Formats an <see cref="IExecutionResult"/> into an SSE stream.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="outputStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        return result switch
        {
            IOperationResult operationResult
                => FormatOperationResultAsync(operationResult, outputStream, cancellationToken),
            OperationResultBatch resultBatch
                => FormatResultBatchAsync(resultBatch, outputStream, cancellationToken),
            IResponseStream responseStream
                => FormatResponseStreamAsync(responseStream, outputStream, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private async ValueTask FormatOperationResultAsync(
        IOperationResult operationResult,
        Stream outputStream,
        CancellationToken ct)
    {
        var buffer = new ArrayWriter();

        var scope = Log.FormatOperationResultStart();
        try
        {
            MessageHelper.WriteNextMessage(_payloadFormatter, operationResult, buffer);
            MessageHelper.WriteCompleteMessage(buffer);

            await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
            await outputStream.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope?.AddError(ex);
            Debug.WriteLine(ex);
        }
        finally
        {
            scope?.Dispose();
            buffer.Dispose();
        }
    }

    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        Stream outputStream,
        CancellationToken ct)
    {
        var writer = PipeWriter.Create(outputStream);
        ArrayWriter? buffer = null;
        KeepAliveJob? keepAlive = null;
        List<Task>? streams = null;

        try
        {
            foreach (var result in resultBatch.Results)
            {
                switch (result)
                {
                    case IOperationResult operationResult:
                        var scope = Log.FormatOperationResultStart();
                        try
                        {
                            buffer ??= new ArrayWriter();
                            MessageHelper.WriteNextMessage(_payloadFormatter, operationResult, buffer);
                            writer.Write(buffer.GetWrittenSpan());
                            await writer.FlushAsync(ct).ConfigureAwait(false);
                            keepAlive?.Reset();
                            buffer.Reset();
                        }
                        catch (Exception ex)
                        {
                            scope?.AddError(ex);
                            Debug.WriteLine(ex);
                        }
                        finally
                        {
                            await operationResult.DisposeAsync().ConfigureAwait(false);
                            scope?.Dispose();
                        }

                        break;

                    case IResponseStream responseStream:
                        keepAlive ??= new KeepAliveJob(writer);
                        streams ??= [];
                        var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, writer);
                        streams.Add(formatter.ProcessAsync(ct));
                        break;

                    default:
                        throw new NotSupportedException(
                            "The result batch contains an unsupported result type.");
                }
            }
        }
        finally
        {
            keepAlive?.Dispose();
            buffer?.Dispose();
        }

        MessageHelper.WriteCompleteMessage(writer);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        var writer = PipeWriter.Create(outputStream);

        using (var keepAlive = new KeepAliveJob(writer))
        {
            var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, writer);
            await formatter.ProcessAsync(ct).ConfigureAwait(false);
        }

        MessageHelper.WriteCompleteMessage(writer);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    private sealed class StreamFormatter(
        JsonResultFormatter payloadFormatter,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        PipeWriter writer)
    {
        public async Task ProcessAsync(CancellationToken ct)
        {
            var buffer = new ArrayWriter();
            try
            {
                await foreach (var result in responseStream.ReadResultsAsync()
                    .WithCancellation(ct)
                    .ConfigureAwait(false))
                {
                    var scope = Log.FormatOperationResultStart();

                    try
                    {
                        MessageHelper.WriteNextMessage(payloadFormatter, result, buffer);
                        writer.Write(buffer.GetWrittenSpan());
                        await writer.FlushAsync(ct).ConfigureAwait(false);
                        keepAliveJob.Reset();
                        buffer.Reset();
                    }
                    catch (Exception ex)
                    {
                        scope?.AddError(ex);
                        Debug.WriteLine(ex);
                        return;
                    }
                    finally
                    {
                        await result.DisposeAsync().ConfigureAwait(false);
                        scope?.Dispose();
                    }
                }
            }
            finally
            {
                await responseStream.DisposeAsync().ConfigureAwait(false);
                buffer.Dispose();
            }
        }
    }

    private sealed class KeepAliveJob : IDisposable
    {
        private static readonly TimeSpan _timerPeriod = TimeSpan.FromSeconds(12);
        private static readonly TimeSpan _keepAlivePeriod = TimeSpan.FromSeconds(8);
        private readonly PipeWriter _writer;
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(PipeWriter writer)
        {
            _writer = writer;
            _keepAliveTimer = new Timer(_ => EnsureKeepAlive(), null, _timerPeriod, _timerPeriod);
        }

        public void Reset() => _lastWriteTime = DateTime.UtcNow;

        private void EnsureKeepAlive()
        {
            if (DateTime.UtcNow - _lastWriteTime >= _keepAlivePeriod)
            {
                Task.Run(WriteKeepAliveAsync);
            }

            return;

            async Task WriteKeepAliveAsync()
            {
                try
                {
                    _writer.Write(MessageHelper.KeepAlive());
                    await _writer.FlushAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _keepAliveTimer.Dispose();
        }
    }

    private static class MessageHelper
    {
        private static readonly byte[] _nextEvent = "event: next\ndata: "u8.ToArray();
        private static readonly byte[] _completeEvent = "event: complete\n\n"u8.ToArray();
        private static readonly byte[] _keepAlive = ":\n\n"u8.ToArray();
        private static readonly byte[] _newLine2 = "\n\n"u8.ToArray();

        public static void WriteNextMessage(
            JsonResultFormatter payloadFormatter,
            IOperationResult result,
            ArrayWriter writer)
        {
            // write the SSE event field
            var span = writer.GetSpan(_nextEvent.Length);
            _nextEvent.CopyTo(span);
            writer.Advance(_nextEvent.Length);

            // write the actual result data
            payloadFormatter.Format(result, writer);

            // write the new line
            span = writer.GetSpan(_newLine2.Length);
            _newLine2.CopyTo(span);
            writer.Advance(_newLine2.Length);
        }

        public static void WriteCompleteMessage(
            IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(_completeEvent.Length);
            _completeEvent.CopyTo(span);
            writer.Advance(_completeEvent.Length);
        }

        public static ReadOnlySpan<byte> KeepAlive() => _keepAlive;
    }
}
