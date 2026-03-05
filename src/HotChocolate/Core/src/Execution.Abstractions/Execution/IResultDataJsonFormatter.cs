using System.Text.Json;
using HotChocolate.Text.Json;

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
    void WriteTo(
        JsonWriter writer,
        JsonSerializerOptions? options = null);
}
