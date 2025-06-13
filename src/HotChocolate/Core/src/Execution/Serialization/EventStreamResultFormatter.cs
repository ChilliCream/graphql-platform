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
    private const int MaxBacklogSize = 64;
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
        await using var writer = new ConcurrentStreamWriter(outputStream, MaxBacklogSize);
        KeepAliveJob? keepAlive = null;
        List<Task>? streams = null;

        await using var tokenRegistration = ct.Register(
            static w => ((ConcurrentStreamWriter)w!).DisposeAsync().FireAndForget(),
            writer,
            useSynchronizationContext: false);

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
                            var buffer = writer.Begin();
                            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, buffer);
                            await writer.CommitAsync(buffer, ct).ConfigureAwait(false);
                            keepAlive?.Reset();
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
            if (streams?.Count > 0)
            {
                await Task.WhenAll(streams).ConfigureAwait(false);
            }

            keepAlive?.Dispose();
        }

        await TryWriteCompleteAsync(writer, ct).ConfigureAwait(false);
        await writer.WaitForCompletionAsync().ConfigureAwait(false);
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        await using var writer = new ConcurrentStreamWriter(outputStream, MaxBacklogSize);

        await using var tokenRegistration = ct.Register(
            static w => ((ConcurrentStreamWriter)w!).DisposeAsync().FireAndForget(),
            writer,
            useSynchronizationContext: false);

        using (var keepAlive = new KeepAliveJob(writer))
        {
            var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, writer);
            await formatter.ProcessAsync(ct).ConfigureAwait(false);
        }

        await TryWriteCompleteAsync(writer, ct).ConfigureAwait(false);
        await writer.WaitForCompletionAsync().ConfigureAwait(false);
    }

    private static async ValueTask TryWriteCompleteAsync(
        ConcurrentStreamWriter writer,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var buffer = writer.Begin();
            MessageHelper.FormatCompleteMessage(buffer);
            await writer.CommitAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private sealed class StreamFormatter(
        JsonResultFormatter payloadFormatter,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        ConcurrentStreamWriter writer)
    {
        public async Task ProcessAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var result in responseStream.ReadResultsAsync()
                    .WithCancellation(ct)
                    .ConfigureAwait(false))
                {
                    var scope = Log.FormatOperationResultStart();

                    try
                    {
                        var buffer = writer.Begin();
                        MessageHelper.FormatNextMessage(payloadFormatter, result, buffer);
                        await writer.CommitAsync(buffer, ct).ConfigureAwait(false);
                        keepAliveJob.Reset();
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
            catch (OperationCanceledException)
            {
                // if the operation was canceled, we do not need to log this
                // and will stop gracefully.
            }
            finally
            {
                await responseStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private sealed class KeepAliveJob : IDisposable
    {
        private static readonly TimeSpan s_timerPeriod = TimeSpan.FromSeconds(12);
        private static readonly TimeSpan s_keepAlivePeriod = TimeSpan.FromSeconds(8);
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _ct;
        private readonly ConcurrentStreamWriter _writer;
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(ConcurrentStreamWriter writer)
        {
            _writer = writer;
            _keepAliveTimer = new Timer(_ => EnsureKeepAlive(), null, s_timerPeriod, s_timerPeriod);
            _ct = _cancellationTokenSource.Token;
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
                    var buffer = _writer.Begin();
                    buffer.Write(MessageHelper.KeepAlive);
                    await _writer.CommitAsync(buffer, _ct).ConfigureAwait(false);
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
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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

        public static ReadOnlySpan<byte> KeepAlive => ":\n\n"u8;
    }
}
