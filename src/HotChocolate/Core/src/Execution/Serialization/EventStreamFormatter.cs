using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.Execution.ExecutionResultKind;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The default GraphQL-SSE formatter for <see cref="IExecutionResult"/>.
/// https://github.com/enisdenjo/graphql-sse/blob/master/PROTOCOL.md
/// </summary>
public sealed class EventStreamFormatter : IExecutionResultFormatter
{
    private static ReadOnlySpan<byte> EventField
        => new[] { (byte)'e', (byte)'v', (byte)'e', (byte)'n', (byte)'t' };
    private static ReadOnlySpan<byte> DataField
        => new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
    private static ReadOnlySpan<byte> NextEvent
        => new[] { (byte)'n', (byte)'e', (byte)'x', (byte)'t' };
    private static ReadOnlySpan<byte> CompleteEvent
        => new[]
        {
            (byte)'c', (byte)'o', (byte)'m', (byte)'p',
            (byte)'l', (byte)'e', (byte)'t', (byte)'e'
        };
    private static readonly byte[] _newLine = new byte[] { (byte)'\n' };

    private readonly JsonQueryResultFormatter _payloadFormatter;
    private readonly JsonWriterOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="EventStreamFormatter" />.
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
    public EventStreamFormatter(
        bool indented = false,
        JavaScriptEncoder? encoder = null)
    {
        _options = new JsonWriterOptions { Indented = indented, Encoder = encoder };
        _payloadFormatter = new JsonQueryResultFormatter(indented, encoder);
    }

    /// <inheritdoc cref="IExecutionResultFormatter.FormatAsync(IExecutionResult, Stream, CancellationToken)" />
    public ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (outputStream is null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        return FormatInternalAsync(result, outputStream, cancellationToken);
    }

    private async ValueTask FormatInternalAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken ct)
    {
        if (result.Kind is SingleResult)
        {
            await WriteNextMessageAsync((IQueryResult)result, outputStream).ConfigureAwait(false);
            await WriteCompleteMessage(outputStream).ConfigureAwait(false);
        }
        else if (result.Kind is DeferredResult or BatchResult or SubscriptionResult)
        {
            var responseStream = (IResponseStream)result;

            await foreach (var queryResult in responseStream.ReadResultsAsync()
                .WithCancellation(ct).ConfigureAwait(false))
            {
                try
                {
                    await WriteNextMessageAsync(queryResult, outputStream)
                        .ConfigureAwait(false);
                }
                finally
                {
                    await queryResult.DisposeAsync().ConfigureAwait(false);
                }

                await WriteNewLineAndFlushAsync(outputStream, ct);
            }

            await WriteCompleteMessage(outputStream).ConfigureAwait(false);
            await WriteNewLineAndFlushAsync(outputStream, ct);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private async ValueTask WriteNextMessageAsync(IQueryResult result, Stream outputStream)
    {
        await using var writer = new Utf8JsonWriter(outputStream, _options);

        writer.WriteStartObject();

        writer.WriteString(EventField, NextEvent);

        writer.WritePropertyName(DataField);
        _payloadFormatter.Format(result, writer);

        writer.WriteEndObject();
    }

    private async ValueTask WriteCompleteMessage(Stream outputStream)
    {
        await using var writer = new Utf8JsonWriter(outputStream, _options);

        writer.WriteStartObject();

        writer.WriteString(EventField, CompleteEvent);

        writer.WriteEndObject();
    }

    private static async ValueTask WriteNewLineAndFlushAsync(
        Stream outputStream,
        CancellationToken ct)
    {
        await outputStream.WriteAsync(_newLine, ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct);
    }
}
