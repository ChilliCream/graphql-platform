using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using HotChocolate.Buffers;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization;

public sealed class JsonLinesResultFormatter(JsonResultFormatterOptions options) : IExecutionResultFormatter
{
    private readonly JsonResultFormatter _payloadFormatter = new(options with { Indented = false });

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

        var scope = EventStreamResultFormatterEventSource.Log.FormatOperationResultStart();
        try
        {
            MessageHelper.WriteNextMessage(_payloadFormatter, operationResult, buffer);

            if (!ct.IsCancellationRequested)
            {
                await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
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
                        var scope = EventStreamResultFormatterEventSource.Log.FormatOperationResultStart();
                        try
                        {
                            buffer ??= new PooledArrayWriter();
                            MessageHelper.WriteNextMessage(_payloadFormatter, operationResult, buffer);

                            writer.Write(buffer.GetWrittenSpan());

                            if (!ct.IsCancellationRequested)
                            {
                                await writer.FlushAsync(ct).ConfigureAwait(false);
                            }

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

        if (!ct.IsCancellationRequested)
        {
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
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

        if (!ct.IsCancellationRequested)
        {
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    private sealed class StreamFormatter(
        JsonResultFormatter payloadFormatter,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        PipeWriter writer)
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
                    var scope = EventStreamResultFormatterEventSource.Log.FormatOperationResultStart();

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
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(PipeWriter writer)
        {
            _writer = writer;
            _keepAliveTimer = new Timer(_ => EnsureKeepAlive(), null, s_timerPeriod, s_timerPeriod);
        }

        public void Reset() => _lastWriteTime = DateTime.UtcNow;

        private void EnsureKeepAlive()
        {
            if (DateTime.UtcNow - _lastWriteTime >= s_keepAlivePeriod)
            {
                WriteKeepAliveAsync().FireAndForget();
            }

            return;

            async Task WriteKeepAliveAsync()
            {
                try
                {
                    _writer.Write(MessageHelper.KeepAlive);
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
        private const byte NewLine = (byte)'\n';

        public static void WriteNextMessage(
            JsonResultFormatter payloadFormatter,
            IOperationResult result,
            PooledArrayWriter writer)
        {
            // write the result data
            payloadFormatter.Format(result, writer);

            // write the new line
            var span = writer.GetSpan(1);
            span[0] = NewLine;
            writer.Advance(1);
        }

        public static ReadOnlySpan<byte> KeepAlive => " "u8;
    }
}
