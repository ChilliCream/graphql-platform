using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Formatters.EventStreamResultFormatterEventSource;

namespace HotChocolate.Transport.Formatters;

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
    public ValueTask FormatAsync(
        IExecutionResult result,
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        return result switch
        {
            OperationResult operationResult
                => FormatOperationResultAsync(operationResult, writer, cancellationToken),
            OperationResultBatch resultBatch
                => FormatResultBatchAsync(resultBatch, writer, cancellationToken),
            IResponseStream responseStream
                => FormatResponseStreamAsync(responseStream, writer, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private async ValueTask FormatOperationResultAsync(
        OperationResult operationResult,
        PipeWriter writer,
        CancellationToken ct)
    {
        var scope = Log.FormatOperationResultStart();

        try
        {
            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, writer);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope?.AddError(ex);
            throw;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        PipeWriter writer,
        CancellationToken ct)
    {
        Exception? exception = null;
        using var semaphore = new SemaphoreSlim(1, 1);
        List<Task>? streams = null;
        KeepAliveJob? keepAlive = null;

        try
        {
            foreach (var result in resultBatch.Results)
            {
                switch (result)
                {
                    case OperationResult operationResult:
                    {
                        using var scope = Log.FormatOperationResultStart();
                        await semaphore.WaitAsync(ct).ConfigureAwait(false);

                        try
                        {
                            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, writer);
                            await writer.FlushAsync(ct).ConfigureAwait(false);
                            keepAlive?.Reset();
                        }
                        catch (Exception ex)
                        {
                            scope?.AddError(ex);
                        }
                        finally
                        {
                            semaphore.Release();
                            await operationResult.DisposeAsync().ConfigureAwait(false);
                        }

                        break;
                    }

                    case IResponseStream responseStream:
                        keepAlive ??= new KeepAliveJob(semaphore, writer);
                        var formatter = new StreamFormatter(
                            _payloadFormatter,
                            keepAlive,
                            responseStream,
                            semaphore,
                            writer);
                        (streams ??= []).Add(formatter.ProcessAsync(ct));
                        break;

                    default:
                        throw new NotSupportedException(
                            "The result batch contains an unsupported result type.");
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            // if the operation was canceled, we do not need to log this
            // and will stop gracefully.
            exception = ex;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            var streamError = await TryCompleteStreamsAsync(streams).ConfigureAwait(false);
            exception ??= streamError;
            keepAlive?.Dispose();

            // we only try to write a complete message if there is no error.
            if (exception is null)
            {
                await TryWriteCompleteAsync(writer, ct).ConfigureAwait(false);
            }
        }

        // we rethrow any stream exception that happened.
        if (exception is not null)
        {
            throw exception;
        }
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        PipeWriter writer,
        CancellationToken ct)
    {
        Exception? exception = null;
        using var semaphore = new SemaphoreSlim(1, 1);

        try
        {
            using (var keepAlive = new KeepAliveJob(semaphore, writer))
            {
                var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, semaphore, writer);
                await formatter.ProcessAsync(ct).ConfigureAwait(false);
            }

            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            // if the operation was canceled, we do not need to log this
            // and will stop gracefully.
            exception = ex;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            // we only try to write a complete message if there is no error.
            if (exception is null)
            {
                await TryWriteCompleteAsync(writer, ct).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask TryWriteCompleteAsync(
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            MessageHelper.FormatCompleteMessage(writer);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            // we ignore any errors on complete.
        }
    }

    private static ValueTask<Exception?> TryCompleteStreamsAsync(List<Task>? streams = null)
    {
        if (streams is null || streams.Count == 0)
        {
            return default;
        }

        return CompleteStreamsAsync(streams);
    }

    private static async ValueTask<Exception?> CompleteStreamsAsync(List<Task> streams)
    {
        try
        {
            await Task.WhenAll(streams).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private sealed class StreamFormatter(
        JsonResultFormatter payloadFormatter,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        SemaphoreSlim semaphore,
        PipeWriter writer)
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
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                    try
                    {
                        MessageHelper.FormatNextMessage(payloadFormatter, result, writer);
                        await writer.FlushAsync(ct).ConfigureAwait(false);
                        keepAliveJob.Reset();
                    }
                    catch (Exception ex)
                    {
                        scope?.AddError(ex);
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
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
        private readonly SemaphoreSlim _semaphore;
        private readonly PipeWriter _writer;
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(SemaphoreSlim semaphore, PipeWriter writer)
        {
            _semaphore = semaphore;
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
                await _semaphore.WaitAsync(_ct).ConfigureAwait(false);

                try
                {
                    _writer.Write(MessageHelper.KeepAlive);
                    await _writer.FlushAsync(_ct).ConfigureAwait(false);
                    _lastWriteTime = DateTime.UtcNow;
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _semaphore.Release();
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
            OperationResult result,
            IBufferWriter<byte> writer)
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
