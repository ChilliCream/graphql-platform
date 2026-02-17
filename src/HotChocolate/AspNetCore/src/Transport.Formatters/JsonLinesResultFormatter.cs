using System.Buffers;
using System.IO.Pipelines;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Formatters.JsonLinesResultFormatterEventSource;

namespace HotChocolate.Transport.Formatters;

public sealed class JsonLinesResultFormatter(JsonResultFormatterOptions options) : IExecutionResultFormatter
{
    private readonly JsonResultFormatter _payloadFormatter = new(options with { Indented = false });

    /// <summary>
    /// Formats an <see cref="IExecutionResult"/> into an JSONL stream.
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

    /// <summary>
    /// Writes a single GraphQL response and then completes.
    /// </summary>
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

    /// <summary>
    /// Writes all results from a variable batch request into the output stream and co
    /// </summary>
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
                            throw;
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
        catch (OperationCanceledException)
        {
            // if the operation was canceled, we do not need to log this
            // and will stop gracefully.
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
        private const byte NewLine = (byte)'\n';

        public static void FormatNextMessage(
            JsonResultFormatter payloadFormatter,
            OperationResult result,
            IBufferWriter<byte> writer)
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
