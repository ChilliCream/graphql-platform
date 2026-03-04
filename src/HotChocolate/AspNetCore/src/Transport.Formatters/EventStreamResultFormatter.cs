using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using HotChocolate.Execution;
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
        ExecutionResultFormatFlags flags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var useIncrementalRfc1 =
            (flags & ExecutionResultFormatFlags.IncrementalRfc1)
                == ExecutionResultFormatFlags.IncrementalRfc1;

        return result switch
        {
            OperationResult operationResult
                => FormatOperationResultAsync(operationResult, writer, useIncrementalRfc1, cancellationToken),
            OperationResultBatch resultBatch
                => FormatResultBatchAsync(resultBatch, writer, useIncrementalRfc1, cancellationToken),
            IResponseStream responseStream
                => FormatResponseStreamAsync(responseStream, writer, useIncrementalRfc1, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private async ValueTask FormatOperationResultAsync(
        OperationResult operationResult,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken ct)
    {
        OperationResultFormatterContext? formatContext = null;
        var scope = Log.FormatOperationResultStart();

        try
        {
            MessageHelper.FormatNextMessage(
                _payloadFormatter,
                operationResult,
                writer,
                useIncrementalRfc1,
                ref formatContext);
            MessageHelper.FormatCompleteMessage(writer);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope?.AddError(ex);
            throw;
        }
        finally
        {
            formatContext?.Dispose();
            scope?.Dispose();
        }
    }

    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        PipeWriter writer,
        bool useIncrementalRfc1,
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
                            OperationResultFormatterContext? formatContext = null;
                            try
                            {
                                MessageHelper.FormatNextMessage(
                                    _payloadFormatter,
                                    operationResult,
                                    writer,
                                    useIncrementalRfc1,
                                    ref formatContext);
                                await writer.FlushAsync(ct).ConfigureAwait(false);
                                keepAlive?.Reset();
                            }
                            finally
                            {
                                formatContext?.Dispose();
                            }
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
                            useIncrementalRfc1,
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
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken ct)
    {
        Exception? exception = null;
        using var semaphore = new SemaphoreSlim(1, 1);

        try
        {
            using (var keepAlive = new KeepAliveJob(semaphore, writer))
            {
                var formatter = new StreamFormatter(
                    _payloadFormatter,
                    useIncrementalRfc1,
                    keepAlive,
                    responseStream,
                    semaphore,
                    writer);
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
        bool useIncrementalRfc1,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        SemaphoreSlim semaphore,
        PipeWriter writer)
    {
        private OperationResultFormatterContext? _formatContext;

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
                        MessageHelper.FormatNextMessage(
                            payloadFormatter,
                            result,
                            writer,
                            useIncrementalRfc1,
                            ref _formatContext);
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
                _formatContext?.Dispose();
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
                _ = WriteKeepAliveAsync();
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
        private static ReadOnlySpan<byte> NextEvent => "event: next\ndata: "u8;
        private static ReadOnlySpan<byte> CompleteEvent => "event: complete\n\n"u8;
        private static ReadOnlySpan<byte> NewLine2 => "\n\n"u8;

        public static void FormatNextMessage(
            JsonResultFormatter payloadFormatter,
            OperationResult result,
            IBufferWriter<byte> writer,
            bool useIncrementalRfc1,
            ref OperationResultFormatterContext? context)
        {
            // write the SSE event field
            var span = writer.GetSpan(NextEvent.Length);
            NextEvent.CopyTo(span);
            writer.Advance(NextEvent.Length);

            // write the actual result data
            payloadFormatter.Format(result, writer, useIncrementalRfc1, ref context);

            // write the new line
            span = writer.GetSpan(NewLine2.Length);
            NewLine2.CopyTo(span);
            writer.Advance(NewLine2.Length);
        }

        public static void FormatCompleteMessage(
            IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(CompleteEvent.Length);
            CompleteEvent.CopyTo(span);
            writer.Advance(CompleteEvent.Length);
        }

        public static ReadOnlySpan<byte> KeepAlive => ":\n\n"u8;
    }
}
