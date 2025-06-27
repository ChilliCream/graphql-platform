using System.Text.Json;

namespace HotChocolate.Execution;

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
    /// <param name="nullIgnoreCondition">
    /// The null ignore condition.
    /// </param>
    public abstract void FormatValue(
        Utf8JsonWriter writer,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition);

    void IResultDataJsonFormatter.WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options,
        JsonNullIgnoreCondition nullIgnoreCondition)
#if NET9_0_OR_GREATER
        => FormatValue(writer, options ?? JsonSerializerOptions.Web, nullIgnoreCondition);
#else
        => FormatValue(writer, options ?? JsonSerializerOptions.Default, nullIgnoreCondition);
#endif
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
/// <remarks>
/// Initializes a new instance of <see cref="NeedsFormatting{TValue}"/>.
/// </remarks>
/// <param name="value">
/// The value that needs formatting.
/// </param>
internal sealed class NeedsFormatting<TValue>(TValue value) : NeedsFormatting
{
    private readonly TValue _value = value;

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
    /// <param name="nullIgnoreCondition">
    /// The null ignore condition.
    /// </param>
    public override void FormatValue(
        Utf8JsonWriter writer,
        JsonSerializerOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition)
        => JsonSerializer.Serialize(writer, _value, options);

    /// <summary>
    /// Returns the string representation of the inner value.
    /// </summary>
    /// <returns>
    /// The string representation of the inner value.
    /// </returns>
    public override string? ToString() => _value?.ToString();
}
