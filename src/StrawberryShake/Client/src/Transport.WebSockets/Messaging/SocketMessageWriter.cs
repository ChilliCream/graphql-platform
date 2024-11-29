using System.Text.Json;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// A Json message writer that buffers the result locally
/// </summary>
public sealed class SocketMessageWriter
    : RequestWriter,
        IAsyncDisposable
{
    private static readonly JsonWriterOptions _options = new() { SkipValidation = true, };

    /// <summary>
    /// The underlying json writer
    /// </summary>
    public Utf8JsonWriter Writer { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SocketMessageWriter"/>
    /// </summary>
    public SocketMessageWriter()
    {
        Writer = new Utf8JsonWriter(this, _options);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        Writer.Reset();
    }

    /// <summary>
    /// Writes the beginning of a JSON object
    /// </summary>
    public void WriteStartObject()
    {
        Writer.WriteStartObject();
        Writer.Flush();
    }

    /// <summary>
    /// Writes the end of a JSON object
    /// </summary>
    public void WriteEndObject()
    {
        Writer.WriteEndObject();
        Writer.Flush();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Writer.DisposeAsync();
        base.Dispose(true);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Writer.Dispose();
        base.Dispose(disposing);
    }
}
