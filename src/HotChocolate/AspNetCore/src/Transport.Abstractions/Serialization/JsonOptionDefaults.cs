using System.Text.Json;

namespace HotChocolate.Transport.Serialization;

/// <summary>
/// A helper class that contains the default settings for JSON serialization.
/// </summary>
internal static class JsonOptionDefaults
{
    /// <summary>
    /// Gets the default <see cref="JsonWriterOptions"/>.
    /// </summary>
    public static JsonWriterOptions WriterOptions { get; } =
        new() { Indented = false, };

    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } =
        new(JsonSerializerDefaults.Web);
}
