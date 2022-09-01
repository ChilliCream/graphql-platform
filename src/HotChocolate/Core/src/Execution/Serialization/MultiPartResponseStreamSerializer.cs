using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization;

// https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md
public sealed partial class MultiPartResponseStreamSerializer : IResponseStreamSerializer
{
    private readonly IQueryResultSerializer _payloadSerializer;

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResponseStreamSerializer" />.
    /// </summary>
    /// <param name="indented">
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without any extra white space.
    /// </param>
    /// <param name="encoder">
    /// Gets or sets the encoder to use when escaping strings, or null to use the default encoder.
    /// </param>
    public MultiPartResponseStreamSerializer(
        bool indented = false, 
        JavaScriptEncoder? encoder = null)
    {
        _payloadSerializer = new JsonQueryResultSerializer(indented, encoder);
    }

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResponseStreamSerializer" />.
    /// </summary>
    /// <param name="queryResultSerializer">
    /// The serializer that shall be used to serialize query results.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="queryResultSerializer"/> is <c>null</c>.
    /// </exception>
    public MultiPartResponseStreamSerializer(
        IQueryResultSerializer queryResultSerializer)
    {
        _payloadSerializer = queryResultSerializer ?? 
            throw new ArgumentNullException(nameof(queryResultSerializer));
    }

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

    private async Task WriteResponseStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        await WriteNextAsync(outputStream, cancellationToken).ConfigureAwait(false);

        await foreach (IQueryResult result in responseStream.ReadResultsAsync()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            await WriteResultAsync(result, outputStream, cancellationToken)
                .ConfigureAwait(false);
            result.Dispose();

            if (result.HasNext ?? false)
            {
                await WriteNextAsync(outputStream, cancellationToken).ConfigureAwait(false);
                await outputStream.FlushAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // we will exit the foreach even if there are more items left
                // since we were signaled that there are no more items
                break;
            }
        }

        await WriteEndAsync(outputStream, cancellationToken).ConfigureAwait(false);
        await outputStream.FlushAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteResultAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        _payloadSerializer.Serialize(result, writer);

        await WriteResultHeaderAsync(outputStream, cancellationToken)
            .ConfigureAwait(false);

        // The payload is sent, followed by a CRLF.
        await outputStream.WriteAsync(
            writer.GetInternalBuffer(), 0, writer.Length, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteResultHeaderAsync(
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        // Each part of the multipart response must contain a Content-Type header.
        // Similar to the GraphQL specification this specification does not require
        // a specific serialization format. For consistency and ease of notation,
        // examples of the response are given in JSON throughout the spec.
        await outputStream.WriteAsync(
                ContentType, 0, ContentType.Length, cancellationToken)
            .ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);

        // After all headers, an additional CRLF is sent.
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteNextAsync(
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        // Before each part of the multi-part response, a boundary (CRLF, ---, CRLF) is sent.
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);
        await outputStream.WriteAsync(Start, 0, Start.Length, cancellationToken)
            .ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteEndAsync(
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        // After the final payload, the terminating boundary of CRLF followed by
        // ----- followed by CRLF is sent.
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);
        await outputStream.WriteAsync(End, 0, End.Length, cancellationToken)
            .ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, cancellationToken)
            .ConfigureAwait(false);
    }
}
