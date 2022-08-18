using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Serialization;

public sealed class JsonArrayResponseStreamFormatter : IResponseStreamFormatter
{
    private const byte _leftBracket = (byte)'[';
    private const byte _rightBracket = (byte)']';
    private const byte _comma = (byte)',';
    private readonly IQueryResultFormatter _formatter;

    /// <summary>
    /// Creates a new instance of <see cref="JsonArrayResponseStreamFormatter" />.
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
    public JsonArrayResponseStreamFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null)
    {
        _formatter = new JsonQueryResultFormatter(indented, encoder);
    }

    /// <summary>
    /// Creates a new instance of <see cref="JsonArrayResponseStreamFormatter" />.
    /// </summary>
    /// <param name="formatter">
    /// The serializer that shall be used to serialize results.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="formatter"/> is <c>null</c>.
    /// </exception>
    public JsonArrayResponseStreamFormatter(
        IQueryResultFormatter formatter)
    {
        _formatter = formatter ??
            throw new ArgumentNullException(nameof(formatter));
    }

    public Task FormatAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (responseStream == null)
        {
            throw new ArgumentNullException(nameof(responseStream));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        return WriteStreamAsync(responseStream, outputStream, cancellationToken);
    }

    private async Task WriteStreamAsync(
        IResponseStream responseStream,
        Stream outputStream,
        CancellationToken ct)
    {
        var del = false;

        await outputStream.WriteAsync(new[] { _leftBracket }, 0, 1, ct).ConfigureAwait(false);

        await foreach (var result in
            responseStream.ReadResultsAsync().WithCancellation(ct).ConfigureAwait(false))
        {
            try
            {
                await WriteNextResultAsync(result, outputStream, del, ct).ConfigureAwait(false);
                del = true;
            }
            finally
            {
                await result.DisposeAsync(); // ensure that pooled results are returned.
            }
        }

        await outputStream.WriteAsync(new[] { _rightBracket }, 0, 1, ct).ConfigureAwait(false);
    }

    private async Task WriteNextResultAsync(
        IQueryResult result,
        Stream outputStream,
        bool delimiter,
        CancellationToken ct)
    {
        if (delimiter)
        {
            await outputStream.WriteAsync(new[] { _comma }, 0, 1, ct).ConfigureAwait(false);
        }

        await _formatter.FormatAsync(result, outputStream, ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }
}
