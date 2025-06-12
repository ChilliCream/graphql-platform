using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using HotChocolate.Buffers;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Serialization.EventStreamResultFormatterEventSource;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default GraphQL-SSE formatter for <see cref="IExecutionResult"/>.
/// https://github.com/enisdenjo/graphql-sse/blob/master/PROTOCOL.md
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventStreamResultFormatter"/>.
/// </remarks>
/// <param name="options">
/// The options to configure the JSON writer.
/// </param>
public sealed class EventStreamResultFormatter(JsonResultFormatterOptions options) : IExecutionResultFormatter
{
    private readonly JsonResultFormatter _payloadFormatter = new(options);

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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(outputStream);

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
        var buffer = new PooledArrayWriter();

        var scope = Log.FormatOperationResultStart();
        try
        {
            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, buffer);
            MessageHelper.FormatCompleteMessage(buffer);

            if (!ct.IsCancellationRequested)
            {
                await outputStream.WriteAsync(buffer.GetWrittenMemory(), ct).ConfigureAwait(false);
                await outputStream.FlushAsync(ct).ConfigureAwait(false);
            }
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
        using var semaphore = new SemaphoreSlim(1, 1);
        var writer = PipeWriter.Create(outputStream);
        PooledArrayWriter? buffer = null;
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
                            buffer ??= new PooledArrayWriter();
                            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, buffer);
                            await writer.WriteAndFlushAsync(semaphore, buffer, ct).ConfigureAwait(false);

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
                        keepAlive ??= new KeepAliveJob(writer, semaphore);
                        streams ??= [];
                        var formatter = new StreamFormatter(
                            _payloadFormatter,
                            keepAlive,
                            responseStream,
                            writer,
                            semaphore);
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
            if (streams?.Count > 0)
            {
                await Task.WhenAll(streams).ConfigureAwait(false);
            }
            keepAlive?.Dispose();
            buffer?.Dispose();
        }

        if (!ct.IsCancellationRequested)
        {
            MessageHelper.FormatCompleteMessage(writer);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        var writer = PipeWriter.Create(outputStream);

        using (var keepAlive = new KeepAliveJob(writer, semaphore))
        {
            var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, writer, semaphore);
            await formatter.ProcessAsync(ct).ConfigureAwait(false);
        }

        if (!ct.IsCancellationRequested)
        {
            MessageHelper.FormatCompleteMessage(writer);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    private sealed class StreamFormatter(
        JsonResultFormatter payloadFormatter,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        PipeWriter writer,
        SemaphoreSlim semaphore)
    {
        public async Task ProcessAsync(CancellationToken ct)
        {
            var buffer = new PooledArrayWriter();
            try
            {
                await foreach (var result in responseStream.ReadResultsAsync()
                    .WithCancellation(ct)
                    .ConfigureAwait(false))
                {
                    var scope = Log.FormatOperationResultStart();

                    try
                    {
                        MessageHelper.FormatNextMessage(payloadFormatter, result, buffer);
                        await writer.WriteAndFlushAsync(semaphore, buffer, ct).ConfigureAwait(false);
                        keepAliveJob.Reset();
                        buffer.Reset();
                    }
                    catch (Exception ex)
                    {
                        scope?.AddError(ex);
                        Debug.WriteLine(ex);
                        await writer.CompleteAsync(ex).ConfigureAwait(false);
                        return;
                    }
                    finally
                    {
                        await result.DisposeAsync().ConfigureAwait(false);
                        scope?.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // if the operation was canceled, we do not need to log this
                // and will stop gracefully.
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
        private static readonly TimeSpan s_timerPeriod = TimeSpan.FromSeconds(12);
        private static readonly TimeSpan s_keepAlivePeriod = TimeSpan.FromSeconds(8);
        private readonly PipeWriter _writer;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(PipeWriter writer, SemaphoreSlim semaphore)
        {
            _writer = writer;
            _semaphore = semaphore;
            _keepAliveTimer = new Timer(_ => EnsureKeepAlive(), null, s_timerPeriod, s_timerPeriod);
        }

        public void Reset() => _lastWriteTime = DateTime.UtcNow;

        private void EnsureKeepAlive()
        {
            if (_disposed)
            {
                return;
            }

            if (DateTime.UtcNow - _lastWriteTime >= s_keepAlivePeriod)
            {
                WriteKeepAliveAsync().FireAndForget();
            }

            async Task WriteKeepAliveAsync()
            {
                try
                {
                    await _writer.WriteAndFlushAsync(
                        _semaphore,
                        MessageHelper.KeepAlive,
                        CancellationToken.None)
                        .ConfigureAwait(false);
                    _lastWriteTime = DateTime.UtcNow;
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
        private static readonly byte[] s_nextEvent = "event: next\ndata: "u8.ToArray();
        private static readonly byte[] s_completeEvent = "event: complete\n\n"u8.ToArray();
        private static readonly byte[] s_newLine2 = "\n\n"u8.ToArray();

        public static void FormatNextMessage(
            JsonResultFormatter payloadFormatter,
            IOperationResult result,
            PooledArrayWriter writer)
        {
            // write the SSE event field
            var span = writer.GetSpan(s_nextEvent.Length);
            s_nextEvent.CopyTo(span);
            writer.Advance(s_nextEvent.Length);

            // write the actual result data
            payloadFormatter.Format(result, writer);

            // write the new line
            span = writer.GetSpan(s_newLine2.Length);
            s_newLine2.CopyTo(span);
            writer.Advance(s_newLine2.Length);
        }

        public static void FormatCompleteMessage(
            IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(s_completeEvent.Length);
            s_completeEvent.CopyTo(span);
            writer.Advance(s_completeEvent.Length);
        }

        public static byte[] KeepAlive { get; } = ":\n\n"u8.ToArray();
    }
}

file static class PipeWriterExtensions
{
    public static async ValueTask WriteAndFlushAsync(
        this PipeWriter writer,
        SemaphoreSlim semaphore,
        PooledArrayWriter data,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            writer.Write(data.GetWrittenSpan());
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async ValueTask WriteAndFlushAsync(
        this PipeWriter writer,
        SemaphoreSlim semaphore,
        byte[] data,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            writer.Write(data);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
