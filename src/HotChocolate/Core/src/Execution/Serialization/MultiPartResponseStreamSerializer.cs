using System;
using System.Buffers;
using System.Buffers.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization
{
    // https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md
    public sealed partial class MultiPartResponseStreamSerializer
        : IResponseStreamSerializer
    {
        private readonly JsonQueryResultSerializer _payloadSerializer =
            new JsonQueryResultSerializer();

        public Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            if (responseStream is null)
            {
                throw new ArgumentNullException(nameof(responseStream));
            }

            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            return WriteResponseStreamAsync(responseStream, outputStream, cancellationToken);
        }

        public async Task WriteResponseStreamAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            await foreach (IQueryResult result in responseStream.ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                await WriteResultAsync(result, outputStream, cancellationToken)
                    .ConfigureAwait(false);
            }

            // After the last part of the multipart response is sent, the terminating
            // boundary ----- is sent, followed by a CRLF
            await outputStream.WriteAsync(End, 0, End.Length, cancellationToken)
                .ConfigureAwait(false);
            await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WriteResultAsync(
            IQueryResult result,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            using var writer = new ArrayWriter();
            _payloadSerializer.Serialize(result, writer);

            await WriteResultHeaderAsync(outputStream, writer.Length, cancellationToken)
                .ConfigureAwait(false);

            // The payload is sent, followed by two CRLFs.
            await outputStream.WriteAsync(
                writer.GetInternalBuffer(), 0, writer.Length, cancellationToken)
                .ConfigureAwait(false);
            await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                .ConfigureAwait(false);
            await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                .ConfigureAwait(false);
            await outputStream.FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WriteResultHeaderAsync(
            Stream outputStream,
            int contentLength,
            CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(128);

            try
            {
                Utf8Formatter.TryFormat(contentLength, buffer, out var w);

                // Each part of the multipart response must start with --- and a CRLF
                await outputStream.WriteAsync(Start, 0, Start.Length, cancellationToken)
                    .ConfigureAwait(false);
                await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                    .ConfigureAwait(false);

                // Each part of the multipart response must contain a Content-Type header.
                // Similar to the GraphQL specification this specification does not require
                // a specific serialization format. For consistency and ease of notation,
                // examples of the response are given in JSON throughout the spec.
                await outputStream.WriteAsync(
                    ContentType, 0, ContentType.Length, cancellationToken)
                    .ConfigureAwait(false);
                await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                    .ConfigureAwait(false);

                // Each part of the multipart response must contain a Content-Length header.
                // This should be the number of bytes of the payload of the response.
                // It does not include the size of the headers, boundaries,
                // or CRLFs used to separate the content.
                await outputStream.WriteAsync(
                        ContentLength, 0, ContentLength.Length, cancellationToken)
                    .ConfigureAwait(false);
                await outputStream.WriteAsync(buffer, 0, w, cancellationToken)
                    .ConfigureAwait(false);
                await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                    .ConfigureAwait(false);

                // After all headers, an additional CRLF is sent.
                await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
