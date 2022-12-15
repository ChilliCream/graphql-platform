using System;
using System.Text.Json;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// This helper class allows to add raw json values to response objects.
/// The JSON query result formatter will take the inner <see cref="Value"/>
/// and writes it without validation to the JSON response object.
/// </summary>
internal readonly struct RawJsonValue
{
    /// <summary>
    /// Initializes a new instance of <see cref="RawJsonValue"/>.
    /// </summary>
    /// <param name="value">
    /// The raw JSON value.
    /// </param>
    public RawJsonValue(ReadOnlyMemory<byte> value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw JSON value.
    /// </summary>
    public ReadOnlyMemory<byte> Value { get; }
}

/// <summary>
/// This helper class allows us to indicate to the formatters that the inner value
/// has a custom formatter.
/// </summary>
internal abstract class NeedsFormatting
{
    public abstract void FormatValue(Utf8JsonWriter writer, JsonSerializerOptions options);
}

internal sealed class NeedsFormatting<TValue> : NeedsFormatting
{
    private readonly TValue _value;

    public NeedsFormatting(TValue value)
    {
        _value = value;
    }

    public override void FormatValue(Utf8JsonWriter writer, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, _value, options);
}


