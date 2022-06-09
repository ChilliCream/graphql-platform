using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization;

// https://github.com/graphql/graphql-over-http/blob/master/rfcs/IncrementalDelivery.md
public sealed partial class MultiPartResponseStreamFormatter : IResponseStreamFormatter
{
    private readonly IQueryResultFormatter _payloadFormatter;

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResponseStreamFormatter" />.
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
    public MultiPartResponseStreamFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null)
    {
        _payloadFormatter = new JsonQueryResultFormatter(indented, encoder);
    }

    /// <summary>
    /// Creates a new instance of <see cref="MultiPartResponseStreamFormatter" />.
    /// </summary>
    /// <param name="queryResultFormatter">
    /// The serializer that shall be used to serialize query results.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="queryResultFormatter"/> is <c>null</c>.
    /// </exception>
    public MultiPartResponseStreamFormatter(
        IQueryResultFormatter queryResultFormatter)
    {
        _payloadFormatter = queryResultFormatter ??
            throw new ArgumentNullException(nameof(queryResultFormatter));
    }

    public Task FormatAsync(
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
        CancellationToken ct = default)
    {
        await WriteNextAsync(outputStream, ct).ConfigureAwait(false);

        await foreach (IQueryResult result in
            responseStream.ReadResultsAsync().WithCancellation(ct).ConfigureAwait(false))
        {
            try
            {
                await WriteResultAsync(result, outputStream, ct).ConfigureAwait(false);

                if (result.HasNext ?? false)
                {
                    await WriteNextAsync(outputStream, ct).ConfigureAwait(false);
                    await outputStream.FlushAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    // we will exit the foreach even if there are more items left
                    // since we were signaled that there are no more items
                    break;
                }
            }
            finally
            {
                await result.DisposeAsync().ConfigureAwait(false);
            }
        }

        await WriteEndAsync(outputStream, ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }

    private async Task WriteResultAsync(
        IQueryResult result,
        Stream outputStream,
        CancellationToken ct)
    {
        using var writer = new ArrayWriter();
        _payloadFormatter.Format(result, writer);

        await WriteResultHeaderAsync(outputStream, ct).ConfigureAwait(false);

        // The payload is sent, followed by a CRLF.
        var buffer = writer.GetInternalBuffer();
        await outputStream.WriteAsync(buffer, 0, writer.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, ct).ConfigureAwait(false);
    }

    private static async Task WriteResultHeaderAsync(
        Stream outputStream,
        CancellationToken ct)
    {
        // Each part of the multipart response must contain a Content-Type header.
        // Similar to the GraphQL specification this specification does not require
        // a specific serialization format. For consistency and ease of notation,
        // examples of the response are given in JSON throughout the spec.
        await outputStream.WriteAsync(ContentType, 0, ContentType.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, ct).ConfigureAwait(false);

        // After all headers, an additional CRLF is sent.
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, ct).ConfigureAwait(false);
    }

    private static async Task WriteNextAsync(
        Stream outputStream,
        CancellationToken ct)
    {
        // Each part of the multipart response must start with --- and a CRLF
        await outputStream.WriteAsync(Start, 0, Start.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, ct).ConfigureAwait(false);
    }

    private static async Task WriteEndAsync(
        Stream outputStream,
        CancellationToken ct)
    {
        // After the last part of the multipart response is sent, the terminating
        // boundary ----- is sent, followed by a CRLF
        await outputStream.WriteAsync(End, 0, End.Length, ct).ConfigureAwait(false);
        await outputStream.WriteAsync(CrLf, 0, CrLf.Length, ct).ConfigureAwait(false);
    }
}
