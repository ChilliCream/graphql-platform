using System.Text.Json;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// This helper class allows us to indicate to the formatters that the inner value
/// has a custom formatter.
/// </summary>
/// <remarks>
/// The downside of this helper is that we bind it explicitly to JSON.
/// If there were alternative query formatter that use different formats we would get
/// into trouble with this.
///
/// This is also the reason for keeping this internal.
/// </remarks>
internal abstract class NeedsFormatting
{
    /// <summary>
    /// Formats the value as JSON
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <param name="options">
    /// The JSON serializer options.
    /// </param>
    public abstract void FormatValue(Utf8JsonWriter writer, JsonSerializerOptions options);
}

/// <summary>
/// This helper class allows us to indicate to the formatters that the inner value
/// has a custom formatter.
/// </summary>
/// <remarks>
/// The downside of this helper is that we bind it explicitly to JSON.
/// If there were alternative query formatter that use different formats we would get
/// into trouble with this.
///
/// This is also the reason for keeping this internal.
/// </remarks>
internal sealed class NeedsFormatting<TValue> : NeedsFormatting
{
    private readonly TValue _value;

    /// <summary>
    /// Initializes a new instance of <see cref="NeedsFormatting{TValue}"/>.
    /// </summary>
    /// <param name="value">
    /// The value that needs formatting.
    /// </param>
    public NeedsFormatting(TValue value)
    {
        _value = value;
    }

    /// <summary>
    /// The inner value.
    /// </summary>
    public TValue Value => _value;

    /// <summary>
    /// Formats the value as JSON
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <param name="options">
    /// The JSON serializer options.
    /// </param>
    public override void FormatValue(Utf8JsonWriter writer, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, _value, options);

    /// <summary>
    /// Returns the string representation of the inner value.
    /// </summary>
    /// <returns>
    /// The string representation of the inner value.
    /// </returns>
    public override string? ToString() => _value?.ToString();
}
