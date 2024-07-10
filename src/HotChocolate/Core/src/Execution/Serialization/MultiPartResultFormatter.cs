using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default MultiPart formatter for <see cref="IExecutionResult"/>.
/// https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md
/// </summary>
public sealed class MultiPartResultFormatter : IExecutionResultFormatter
{
    private readonly IOperationResultFormatter _payloadFormatter;

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResultFormatter" />.
    /// </summary>
    /// <param name="options">
    /// The JSON result formatter options
    /// </param>
    public MultiPartResultFormatter(JsonResultFormatterOptions options = default)
    {
        _payloadFormatter = new JsonResultFormatter(options);
    }

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResultFormatter" />.
    /// </summary>
    /// <param name="operationResultFormatter">
    /// The serializer that shall be used to serialize query results.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="operationResultFormatter"/> is <c>null</c>.
    /// </exception>
    public MultiPartResultFormatter(IOperationResultFormatter operationResultFormatter)
    {
        _payloadFormatter = operationResultFormatter ??
            throw new ArgumentNullException(nameof(operationResultFormatter));
    }

    /// <summary>
    /// Formats an <see cref="IExecutionResult"/> into a multipart stream.
    /// </summary>
    /// <param name="result">
    /// The result that shall be formatted.
    /// </param>
    /// <param name="outputStream">
    /// The stream to which the result shall be written.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The formatter does not support the specified result.
    /// </exception>
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

        if (result.Kind == SubscriptionResult)
        {
            throw new NotSupportedException(
                "Subscriptions are not supported by this formatter.");
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
        IOperationResult result,
        Stream outputStream,
        CancellationToken ct = default)
    {
        using var buffer = new ArrayWriter();
        MessageHelper.WriteNext(buffer);

        // First we write the header of the part.
        MessageHelper.WriteResultHeader(buffer);

        // Next we write the payload of the part.
        MessageHelper.WritePayload(buffer, result, _payloadFormatter);

        // Last we write the end of the part.
        MessageHelper.WriteEnd(buffer);

        await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }

    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        Stream outputStream,
        CancellationToken ct = default)
    {
        ArrayWriter? buffer = null;
        foreach (var result in resultBatch.Results)
        {
            switch (result)
            {
                case IOperationResult operationResult:
                    buffer ??= new ArrayWriter();
                    MessageHelper.WriteNext(buffer);
                    MessageHelper.WriteResultHeader(buffer);
                    MessageHelper.WritePayload(buffer, operationResult, _payloadFormatter);
                    MessageHelper.WriteEnd(buffer);
                    await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
                    await outputStream.FlushAsync(ct).ConfigureAwait(false);
                    break;

                case IResponseStream responseStream:
                    await FormatResponseStreamAsync(responseStream, outputStream, ct).ConfigureAwait(false);
                    break;
            }
        }

        buffer?.Dispose();
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct = default)
    {
        // first we create the iterator.
        using var buffer = new ArrayWriter();
        await using var enumerator = responseStream.ReadResultsAsync().GetAsyncEnumerator(ct);
        var first = true;

        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            try
            {
                buffer.Reset();

                if (first || responseStream.Kind is not DeferredResult)
                {
                    MessageHelper.WriteNext(buffer);
                    first = false;
                }

                // First we write the header of the part.
                MessageHelper.WriteResultHeader(buffer);

                // Next we write the payload of the part.
                MessageHelper.WritePayload(buffer, enumerator.Current, _payloadFormatter);

                if (responseStream.Kind is DeferredResult && (enumerator.Current.HasNext ?? false))
                {
                    // If the result is a deferred result and has a next result we need to
                    // write a new part so that the client knows that there is more to come.
                    MessageHelper.WriteNext(buffer);
                }

                // Now we can write the part to the output stream and flush this chunk.
                await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
                await outputStream.FlushAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                // The result objects use pooled memory so we need to ensure that they
                // return the memory by disposing them.
                await enumerator.Current.DisposeAsync().ConfigureAwait(false);
            }
        }

        // After all parts have been written we need to write the final boundary.
        buffer.Reset();
        MessageHelper.WriteEnd(buffer);
        await outputStream.WriteAsync(buffer.GetInternalBuffer(), 0, buffer.Length, ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static class MessageHelper
    {
        private static ReadOnlySpan<byte> ContentType => "Content-Type: application/json; charset=utf-8\r\n\r\n"u8;
        private static ReadOnlySpan<byte> Start => "\r\n---\r\n"u8;
        private static ReadOnlySpan<byte> End => "\r\n-----\r\n"u8;
        private static ReadOnlySpan<byte> CrLf => "\r\n"u8;

        public static void WriteNext(IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(Start.Length);
            Start.CopyTo(span);
            writer.Advance(Start.Length);
        }

        public static void WriteEnd(IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(End.Length);
            End.CopyTo(span);
            writer.Advance(End.Length);
        }

        public static void WritePayload(
            IBufferWriter<byte> writer,
            IOperationResult result,
            IOperationResultFormatter payloadFormatter)
            => payloadFormatter.Format(result, writer);

        public static void WriteResultHeader(IBufferWriter<byte> writer)
        {
            // Each part of the multipart response must contain a Content-Type header.
            // Similar to the GraphQL specification this specification does not require
            // a specific serialization format. For consistency and ease of notation,
            // examples of the response are given in JSON throughout the spec.
            // After all headers, an additional CRLF is sent.
            var span = writer.GetSpan(ContentType.Length);
            ContentType.CopyTo(span);
            writer.Advance(ContentType.Length);
        }
    }
}