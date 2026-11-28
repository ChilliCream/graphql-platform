using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using HotChocolate.Execution;
using static HotChocolate.Transport.Formatters.JsonLinesResultFormatterEventSource;

namespace HotChocolate.Transport.Formatters;

public sealed class JsonLinesResultFormatter(JsonResultFormatterOptions options) : IExecutionResultFormatter
{
    /// <summary>
    /// The number of unflushed bytes from which the results of a batch or of a response stream
    /// are flushed even though a further result is ready.
    /// </summary>
    internal const int FlushThreshold = 64 * 1024;

    private readonly JsonResultFormatter _payloadFormatter = new(options with { Indented = false });

    /// <summary>
    /// Formats an <see cref="IExecutionResult"/> into an JSONL stream.
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

    /// <summary>
    /// Writes a single GraphQL response and then completes.
    /// </summary>
    private async ValueTask FormatOperationResultAsync(
        OperationResult operationResult,
        PipeWriter pipe,
        bool useIncrementalRfc1,
        CancellationToken ct)
    {
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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

    /// <summary>
    /// Writes all results from a variable batch request into the output stream and
    /// completes when all results have been written.
    /// </summary>
    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        PipeWriter pipe,
        bool useIncrementalRfc1,
        CancellationToken ct)
    {
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: true);
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
                        await semaphore.WaitAsync(ct).ConfigureAwait(false);

                        try
                        {
                            await WriteBatchResultAsync(operationResult, writer, useIncrementalRfc1)
                                .ConfigureAwait(false);

                            if (writer.BytesSinceFlush >= FlushThreshold)
                            {
                                await writer.FlushAsync(ct).ConfigureAwait(false);
                                keepAlive?.Reset();
                            }
                        }
                        catch when (writer.BytesSinceFlush > 0)
                        {
                            // the results that were written before the error are still sent.
                            try
                            {
                                await writer.FlushAsync(ct).ConfigureAwait(false);
                            }
                            catch
                            {
                                // the first error is the one to report.
                            }

                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                        break;

                    case IResponseStream responseStream:
                        keepAlive ??= new KeepAliveJob(semaphore, pipe);
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

            // the results of the batch are sent before the formatter waits for its streams.
            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (writer.BytesSinceFlush > 0)
                {
                    await writer.FlushAsync(ct).ConfigureAwait(false);
                    keepAlive?.Reset();
                }
            }
            finally
            {
                semaphore.Release();
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
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    /// Writes a result of a batch to <paramref name="writer"/> without flushing it
    /// and disposes the result.
    /// </summary>
    private async ValueTask WriteBatchResultAsync(
        OperationResult result,
        ChunkedPipeBufferWriter writer,
        bool useIncrementalRfc1)
    {
        OperationResultFormatterContext? formatContext = null;
        var scope = Log.FormatOperationResultStart();

        try
        {
            MessageHelper.FormatNextMessage(
                _payloadFormatter,
                result,
                writer,
                useIncrementalRfc1,
                ref formatContext);
            writer.Commit();
        }
        catch (Exception ex)
        {
            scope?.AddError(ex);
            writer.Discard();
            throw;
        }
        finally
        {
            formatContext?.Dispose();
            await result.DisposeAsync().ConfigureAwait(false);
            scope?.Dispose();
        }
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        PipeWriter pipe,
        bool useIncrementalRfc1,
        CancellationToken ct)
    {
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: true);
        using var semaphore = new SemaphoreSlim(1, 1);

        try
        {
            using (var keepAlive = new KeepAliveJob(semaphore, pipe))
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
        bool useIncrementalRfc1,
        KeepAliveJob keepAliveJob,
        IResponseStream responseStream,
        SemaphoreSlim semaphore,
        ChunkedPipeBufferWriter writer)
    {
        private OperationResultFormatterContext? _formatContext;

        public async Task ProcessAsync(CancellationToken ct)
        {
            try
            {
                var results = responseStream.ReadResultsAsync().GetAsyncEnumerator(ct);

                await using (results.ConfigureAwait(false))
                {
                    // a subscription computes its next result while the stream moves to it.
                    var flushEachResult = responseStream.Kind is ExecutionResultKind.SubscriptionResult;
                    var hasResult = await results.MoveNextAsync().ConfigureAwait(false);

                    while (hasResult)
                    {
                        hasResult = flushEachResult
                            ? await WriteResultAndMoveAsync(results, ct).ConfigureAwait(false)
                            : await WriteReadyResultsAsync(results, ct).ConfigureAwait(false);
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

        /// <summary>
        /// Writes and flushes the current result, then moves the stream to its next result
        /// and returns whether it has one.
        /// </summary>
        private async ValueTask<bool> WriteResultAndMoveAsync(
            IAsyncEnumerator<OperationResult> results,
            CancellationToken ct)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                await WriteResultAsync(results.Current).ConfigureAwait(false);
                await writer.FlushAsync(ct).ConfigureAwait(false);
                keepAliveJob.Reset();
            }
            finally
            {
                semaphore.Release();
            }

            return await results.MoveNextAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the current result and the results that are ready after it, flushes them
        /// together and returns whether the stream has a further result.
        /// </summary>
        private async ValueTask<bool> WriteReadyResultsAsync(
            IAsyncEnumerator<OperationResult> results,
            CancellationToken ct)
        {
            ValueTask<bool>? pendingNext = null;
            var hasResult = true;

            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                try
                {
                    do
                    {
                        await WriteResultAsync(results.Current).ConfigureAwait(false);
                        var next = results.MoveNextAsync();

                        if (next.IsCompletedSuccessfully)
                        {
                            hasResult = next.Result;
                        }
                        else
                        {
                            pendingNext = next;
                        }
                    }
                    while (hasResult && pendingNext is null && writer.BytesSinceFlush < FlushThreshold);

                    await writer.FlushAsync(ct).ConfigureAwait(false);
                    keepAliveJob.Reset();
                }
                catch when (pendingNext is null && writer.BytesSinceFlush > 0)
                {
                    // the results that were written before the error are still sent.
                    try
                    {
                        await writer.FlushAsync(ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        // the first error is the one to report.
                    }

                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch when (pendingNext is not null)
            {
                // the stream cannot be disposed while it moves to its next result.
                await DropNextResultAsync(results, pendingNext.GetValueOrDefault()).ConfigureAwait(false);
                throw;
            }

            return pendingNext is { } pending
                ? await pending.ConfigureAwait(false)
                : hasResult;
        }

        /// <summary>
        /// Waits for the pending move of the stream and disposes the result it yields, if any.
        /// </summary>
        private static async ValueTask DropNextResultAsync(
            IAsyncEnumerator<OperationResult> results,
            ValueTask<bool> next)
        {
            try
            {
                if (await next.ConfigureAwait(false))
                {
                    await results.Current.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // the error that stopped the formatter is the one to report.
            }
        }

        private async ValueTask WriteResultAsync(OperationResult result)
        {
            var scope = Log.FormatOperationResultStart();

            try
            {
                MessageHelper.FormatNextMessage(
                    payloadFormatter,
                    result,
                    writer,
                    useIncrementalRfc1,
                    ref _formatContext);
                writer.Commit();
            }
            catch (Exception ex)
            {
                scope?.AddError(ex);
                writer.Discard();
                throw;
            }
            finally
            {
                await result.DisposeAsync().ConfigureAwait(false);
                scope?.Dispose();
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
        private const byte NewLine = (byte)'\n';

        public static void FormatNextMessage(
            JsonResultFormatter payloadFormatter,
            OperationResult result,
            IBufferWriter<byte> writer,
            bool useIncrementalRfc1,
            ref OperationResultFormatterContext? context)
        {
            // write the result data
            payloadFormatter.Format(result, writer, useIncrementalRfc1, ref context);

            // write the new line
            var span = writer.GetSpan(1);
            span[0] = NewLine;
            writer.Advance(1);
        }

        public static ReadOnlySpan<byte> KeepAlive => " \n"u8;
    }
}
