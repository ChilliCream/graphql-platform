using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Formatters.JsonLinesResultFormatterEventSource;

namespace HotChocolate.Transport.Formatters;

public sealed class JsonLinesResultFormatter(JsonResultFormatterOptions options) : IExecutionResultFormatter
{
    private const int MaxBacklogSize = 64;
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

    /// <summary>
    /// Writes a single GraphQL response and then completes.
    /// </summary>
    private async ValueTask FormatOperationResultAsync(
        IOperationResult operationResult,
        Stream outputStream,
        CancellationToken ct)
    {
        Exception? exception = null;
        var writer = outputStream.CreatePipeWriter();
        var scope = Log.FormatOperationResultStart();

        try
        {
            MessageHelper.FormatNextMessage(_payloadFormatter, operationResult, writer);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope?.AddError(ex);
            exception = ex;
            throw;
        }
        finally
        {
            scope?.Dispose();
            await writer.CompleteAsync(exception).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes all results from a variable batch request into the output stream and co
    /// </summary>
    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        Stream outputStream,
        CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        var writer = outputStream.CreatePipeWriter();

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

        await writer.WaitForCompletionAsync().ConfigureAwait(false);
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        Exception? exception = null;
        using var semaphore = new SemaphoreSlim(1, 1);
        var writer = outputStream.CreatePipeWriter();

        try
        {
            using (var keepAlive = new KeepAliveJob(semaphore, writer))
            {
                var formatter = new StreamFormatter(_payloadFormatter, keepAlive, responseStream, semaphore, writer);
                await formatter.ProcessAsync(ct).ConfigureAwait(false);
            }

            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            await writer.CompleteAsync(exception).ConfigureAwait(false);
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
                        keepAliveJob.Reset();
                        await writer.FlushAsync(ct).ConfigureAwait(false);
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
        private readonly SemaphoreSlim _semaphore;
        private readonly IBufferWriter<byte> _writer;
        private readonly Timer _keepAliveTimer;
        private DateTime _lastWriteTime = DateTime.UtcNow;
        private bool _disposed;

        public KeepAliveJob(SemaphoreSlim semaphore, IBufferWriter<byte> writer)
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
            IOperationResult result,
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
