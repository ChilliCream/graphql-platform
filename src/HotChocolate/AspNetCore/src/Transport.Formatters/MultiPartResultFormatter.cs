using System.Buffers;
using System.IO.Pipelines;
using HotChocolate.Execution;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.Transport.Formatters;

/// <summary>
/// The default MultiPart formatter for <see cref="IExecutionResult"/>.
/// https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md
/// </summary>
public sealed class MultiPartResultFormatter : IExecutionResultFormatter
{
    private readonly JsonResultFormatter _payloadFormatter;

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
    /// <param name="jsonResultFormatter">
    /// The serializer that shall be used to serialize query results.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="jsonResultFormatter"/> is <c>null</c>.
    /// </exception>
    public MultiPartResultFormatter(JsonResultFormatter jsonResultFormatter)
    {
        ArgumentNullException.ThrowIfNull(jsonResultFormatter);
        _payloadFormatter = jsonResultFormatter;
    }

    /// <summary>
    /// Formats an <see cref="IExecutionResult"/> into a multipart stream.
    /// </summary>
    public ValueTask FormatAsync(
        IExecutionResult result,
        PipeWriter writer,
        ExecutionResultFormatFlags flags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (result.Kind == SubscriptionResult)
        {
            throw new NotSupportedException(
                "Subscriptions are not supported by this formatter.");
        }

        var useIncrementalRfc1 = (flags & ExecutionResultFormatFlags.IncrementalRfc1) == ExecutionResultFormatFlags.IncrementalRfc1;

        return result switch
        {
            OperationResult operationResult
                => FormatOperationResultAsync(operationResult, writer, useIncrementalRfc1, cancellationToken),
            OperationResultBatch resultBatch
                => FormatResultBatchAsync(resultBatch, writer, useIncrementalRfc1, cancellationToken),
            IResponseStream responseStream
                => FormatResponseStreamAsync(
                    responseStream,
                    writer,
                    useIncrementalRfc1,
                    cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private async ValueTask FormatOperationResultAsync(
        OperationResult result,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken ct = default)
    {
        OperationResultFormatterContext? formatContext = null;
        try
        {
            MessageHelper.WriteNext(writer);

            // First, we write the header of the part.
            MessageHelper.WriteResultHeader(writer);

            // Next, we write the payload of the part.
            MessageHelper.WritePayload(
                writer,
                result,
                _payloadFormatter,
                useIncrementalRfc1,
                ref formatContext);

            // Last we write the end of the part.
            MessageHelper.WriteEnd(writer);

            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            formatContext?.Dispose();
        }
    }

    private async ValueTask FormatResultBatchAsync(
        OperationResultBatch resultBatch,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken ct = default)
    {
        foreach (var result in resultBatch.Results)
        {
            switch (result)
            {
                case OperationResult operationResult:
                    try
                    {
                        OperationResultFormatterContext? formatContext = null;
                        try
                        {
                            MessageHelper.WriteNext(writer);
                            MessageHelper.WriteResultHeader(writer);
                            MessageHelper.WritePayload(
                                writer,
                                operationResult,
                                _payloadFormatter,
                                useIncrementalRfc1,
                                ref formatContext);
                            MessageHelper.WriteEnd(writer);
                            await writer.FlushAsync(ct).ConfigureAwait(false);
                        }
                        finally
                        {
                            formatContext?.Dispose();
                        }
                    }
                    finally
                    {
                        await operationResult.DisposeAsync().ConfigureAwait(false);
                    }

                    break;

                case IResponseStream responseStream:
                    await FormatResponseStreamAsync(
                        responseStream,
                        writer,
                        useIncrementalRfc1,
                        ct).ConfigureAwait(false);
                    break;
            }
        }
    }

    private async ValueTask FormatResponseStreamAsync(
        IResponseStream responseStream,
        PipeWriter writer,
        bool useIncrementalRfc1,
        CancellationToken ct = default)
    {
        // first, we create the iterator.
        await using var enumerator = responseStream.ReadResultsAsync().GetAsyncEnumerator(ct);
        OperationResultFormatterContext? formatContext = null;
        var first = true;

        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var current = enumerator.Current;

                try
                {
                    if (first || responseStream.Kind is not DeferredResult)
                    {
                        MessageHelper.WriteNext(writer);
                        first = false;
                    }

                    // First, we write the header of the part.
                    MessageHelper.WriteResultHeader(writer);

                    // Next, we write the payload of the part.
                    MessageHelper.WritePayload(
                        writer,
                        current,
                        _payloadFormatter,
                        useIncrementalRfc1,
                        ref formatContext);

                    if (responseStream.Kind is DeferredResult && (current.HasNext ?? false))
                    {
                        // If the result is a deferred result and has a next result, we need to
                        // write a new part so that the client knows that there is more to come.
                        MessageHelper.WriteNext(writer);
                    }

                    // Now we can write the part to the output stream and flush this chunk.
                    await writer.FlushAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    // The result objects use pooled memory, so we need to ensure that they
                    // return the memory by disposing them.
                    await current.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            formatContext?.Dispose();
        }

        // After all parts have been written, we need to write the final boundary.
        MessageHelper.WriteEnd(writer);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    private static class MessageHelper
    {
        private static ReadOnlySpan<byte> ContentType => "Content-Type: application/json; charset=utf-8\r\n\r\n"u8;
        private static ReadOnlySpan<byte> Start => "\r\n---\r\n"u8;
        private static ReadOnlySpan<byte> End => "\r\n-----\r\n"u8;

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
            OperationResult result,
            JsonResultFormatter payloadFormatter,
            bool useIncrementalRfc1,
            ref OperationResultFormatterContext? context)
            => payloadFormatter.Format(result, writer, useIncrementalRfc1, ref context);

        public static void WriteResultHeader(IBufferWriter<byte> writer)
        {
            // Each part of the multipart response must contain a Content-Type header.
            // Similar to the GraphQL specification, this specification does not require
            // a specific serialization format. For consistency and ease of notation,
            // examples of the response are given in JSON throughout the spec.
            // After all headers, an additional CRLF is sent.
            var span = writer.GetSpan(ContentType.Length);
            ContentType.CopyTo(span);
            writer.Advance(ContentType.Length);
        }
    }
}
