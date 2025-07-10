using System.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a JSON formatter for result data objects.
/// </summary>
public interface IResultDataJsonFormatter
{
    /// <summary>
    /// Writes the result data to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">
    /// The writer to write the result data to.
    /// </param>
    /// <param name="options">
    /// The serializer options.
    /// If options are set to null <see cref="JsonSerializerOptions"/>.Web will be used.
    /// </param>
    /// <param name="nullIgnoreCondition">
    /// The null ignore condition.
    /// </param>
    void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None);
}
