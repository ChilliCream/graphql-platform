using System.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// This helper class allows us to indicate to the formatters that the inner value
/// has a custom formatter.
/// </summary>
/// <remarks>
/// <para>
/// The downside of this helper is that we bind it explicitly to JSON.
/// If there were alternative query formatter that use different formats we would get
/// into trouble with this.
/// </para>
/// <para>This is also the reason for keeping this internal.</para>
/// </remarks>
internal abstract class NeedsFormatting : IResultDataJsonFormatter
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
    public abstract void FormatValue(
        JsonWriter writer,
        JsonSerializerOptions options);

    void IResultDataJsonFormatter.WriteTo(
        JsonWriter writer,
        JsonSerializerOptions? options)
        => FormatValue(writer, options ?? JsonSerializerOptionDefaults.GraphQL);

    public static JsonNeedsFormatting Create<TValue>(TValue value)
    {
        var documents = JsonSerializer.SerializeToDocument(value, JsonSerializerOptionDefaults.GraphQL);
        return new JsonNeedsFormatting(documents);
    }
}

/// <summary>
/// This helper class allows us to indicate to the formatters that the inner value
/// has a custom formatter.
/// </summary>
/// <remarks>
/// <para>
/// The downside of this helper is that we bind it explicitly to JSON.
/// If there was an alternative query formatter that uses different formats, we would get
/// into trouble with this.
/// </para>
/// <para>
/// This is also the reason for keeping this internal.
/// </para>
/// </remarks>
/// <param name="value">
/// The value that needs formatting.
/// </param>
internal sealed class JsonNeedsFormatting(JsonDocument value) : NeedsFormatting
{
    /// <summary>
    /// The inner value.
    /// </summary>
    public JsonDocument Value { get; } = value;

    /// <summary>
    /// Formats the value as JSON
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <param name="options">
    /// The JSON serializer options.
    /// </param>
    public override void FormatValue(
        JsonWriter writer,
        JsonSerializerOptions options)
        => JsonValueFormatter.WriteValue(writer, Value, options);

    /// <summary>
    /// Returns the string representation of the inner value.
    /// </summary>
    /// <returns>
    /// The string representation of the inner value.
    /// </returns>
    public override string? ToString() => Value.ToString();
}
